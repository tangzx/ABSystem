using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tangzx.ABSystem
{
    public class AssetTarget : AssetBundleEntry, System.IComparable<AssetTarget>
    {
        /// <summary>
        /// 目标Object
        /// </summary>
        public Object asset;
        /// <summary>
        /// 文件路径
        /// </summary>
        public FileInfo file;
        /// <summary>
        /// 此文件是否已导出
        /// </summary>
        public bool isExported;

        public int level = -1;
        public List<AssetTarget> levelList;

        //目标文件是否已改变
        private bool _isFileChanged;
        //是否已分析过依赖
        private bool _isAnalyzed;
        //上次打包的信息（用于增量打包）
        private AssetCacheInfo _cacheInfo;
        //.meta 文件的Hash
        private string _metaHash;
        //上次打好的AB的CRC值（用于增量打包）
        private string _bundleCrc;
        //是否是新打包的
        private bool _isNewBuild;

        public AssetTarget(Object o, FileInfo file, string assetPath)
        {
            asset = o;
            this.file = file;
            this.assetPath = assetPath;
            bundleShortName = file.Name.ToLower();
            bundleName = HashUtil.Get(AssetBundleUtils.ConvertToABName(assetPath)) + ".ab";

            _isFileChanged = true;
            _metaHash = "0";
        }

        /// <summary>
        /// Texture
        /// AudioClip
        /// Mesh
        /// Model
        /// Shader
        /// 这些类型的Asset的一配置是放在.meta中的，所以要监视它们的变化
        /// 而在5x中系统会自己处理的，不用管啦
        /// </summary>
        void LoadMetaHashIfNecessary()
        {
            bool shouldLoad = asset is Texture ||
                asset is AudioClip ||
                asset is Mesh ||
                asset is Shader;

            if (!shouldLoad)
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                shouldLoad = importer is ModelImporter;
            }

            if (shouldLoad)
            {
                _metaHash = AssetBundleUtils.GetFileHash(assetPath + ".meta");
            }
        }

        /// <summary>
        /// 分析引用关系
        /// </summary>
        public void Analyze()
        {
            if (_isAnalyzed) return;
            _isAnalyzed = true;

#if !UNITY_5
            LoadMetaHashIfNecessary();
#endif
            _cacheInfo = AssetBundleUtils.GetCacheInfo(assetPath);
            _isFileChanged = _cacheInfo == null || !_cacheInfo.fileHash.Equals(GetHash()) || !_cacheInfo.metaHash.Equals(_metaHash);
            if (_cacheInfo != null)
            {
                _bundleCrc = _cacheInfo.bundleCrc;
                if (_isFileChanged)
                    Debug.Log("File was changed : " + assetPath);
            }

            Object[] deps = EditorUtility.CollectDependencies(new Object[] { asset });
#if UNITY_5
            List<Object> depList = new List<Object>();
            for (int i = 0; i < deps.Length; i++)
            {
                Object o = deps[i];
                //不包含脚本对象
                //不包含LightingDataAsset对象
                if (o is MonoScript || o is LightingDataAsset)
                    continue;

                //不包含builtin对象
                string path = AssetDatabase.GetAssetPath(o);
                if (path.StartsWith("Resources"))
                    continue;

                depList.Add(o);
            }
            deps = depList.ToArray();
#else
            //提取 resource.builtin
            for (int i = 0; i < deps.Length; i++)
            {
                Object dep = deps[i];
                string path = AssetDatabase.GetAssetPath(dep);
                if (path.StartsWith("Resources"))
                {
                    AssetTarget builtinAsset = AssetBundleUtils.Load(dep);
                    this.AddDependParent(builtinAsset);
                    builtinAsset.Analyze();
                }
            }
#endif

            var res = from s in deps
                      let obj = AssetDatabase.GetAssetPath(s)
                      select obj;
            var paths = res.Distinct().ToArray();

            for (int i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i]) == false)
                {
                    //Debug.Log("invalid:" + paths[i]);
                    continue;
                }
                FileInfo fi = new FileInfo(paths[i]);
                AssetTarget target = AssetBundleUtils.Load(fi);
                if (target == null)
                    continue;

                AddDependParent(target);

                target.Analyze();
            }
        }

        public void UpdateLevel(int level, List<AssetTarget> lvList)
        {
            this.level = level;
            if (level == -1 && levelList != null)
                levelList.Remove(this);
            levelList = lvList;
        }
        
        public bool IsNewBuild
        {
            get { return _isNewBuild; }
        }

        public override string BundleCrc
        {
            get { return _bundleCrc; }
            set
            {
                _isNewBuild = value != _bundleCrc;
                if (_isNewBuild)
                {
                    Debug.Log("Export AB : " + bundleName);
                }
                _bundleCrc = value;
            }
        }

        int System.IComparable<AssetTarget>.CompareTo(AssetTarget other)
        {
            return other.childCount.CompareTo(childCount);
        }

        public string GetHash()
        {
            return AssetBundleUtils.GetFileHash(file.FullName);
        }

#if UNITY_4 || UNITY_4_6
        public void BuildBundle(BuildAssetBundleOptions options)
        {
            string savePath = Path.Combine(Path.GetTempPath(), bundleName);

            this.isExported = true;

            var children = dependencies;

            Object[] assets = new Object[children.Count + 1];
            assets[0] = asset;

            for (int i = 0; i < children.Count; i++)
            {
                assets[i + 1] = children[i].asset;
            }

            uint crc = 0;
            if (file.Extension.EndsWith("unity"))
            {
                BuildPipeline.BuildStreamedSceneAssetBundle(
                    new string[] { file.FullName },
                    savePath,
                    EditorUserBuildSettings.activeBuildTarget,
                    out crc,
                    BuildOptions.UncompressedAssetBundle);
            }
            else
            {
                BuildPipeline.BuildAssetBundle(
                    asset,
                    assets,
                    savePath,
                    out crc,
                    options,
                    EditorUserBuildSettings.activeBuildTarget);
            }

            bundleCrc = crc.ToString();

            if (_isNewBuild)
                File.Copy(savePath, bundleSavePath, true);
        }
#endif

        public void WriteCache(StreamWriter sw)
        {
            sw.WriteLine(assetPath);
            sw.WriteLine(GetHash());
            sw.WriteLine(_metaHash);
            sw.WriteLine(_bundleCrc);
            HashSet<AssetBundleEntry> deps = new HashSet<AssetBundleEntry>();
            GetDependencies(deps);
            sw.WriteLine(deps.Count.ToString());
            foreach (AssetBundleEntry at in deps)
            {
                sw.WriteLine(at.assetPath);
            }
        }
    }
}
