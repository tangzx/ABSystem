using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Uzen.AB
{
    enum AssetType
    {
        Asset,
        UI,
        BattleScene,
        Model,
        Builtin
    }

    enum ExportType
    {
        Asset = 0,
        /// <summary>
        /// 根
        /// </summary>
        Root = 1,
        /// <summary>
        /// 单独打包
        /// </summary>
        Standalone = 1 << 2
    }

    class AssetTarget : System.IComparable<AssetTarget>
    {
        const int MinTexturePixels = 128 * 128;
        const float MinAudioClipLength = 2f;

        /// <summary>
        /// 目标Object
        /// </summary>
        public Object asset;
        /// <summary>
        /// 文件路径
        /// </summary>
        public FileInfo file;
        /// <summary>
        /// 相对于Assets文件夹的目录
        /// </summary>
        public string assetPath;
        /// <summary>
        /// 此文件是否已导出
        /// </summary>
        public bool isExported;
        /// <summary>
        /// 素材类型
        /// </summary>
        public AssetType type = AssetType.Asset;
        /// <summary>
        /// 导出类型
        /// </summary>
        public ExportType exportType = ExportType.Asset;
        /// <summary>
        /// 保存地址
        /// </summary>
        public string bundleSavePath;
        public string bundleName;

        public int level = -1;
        public List<AssetTarget> levelList;

        //目标文件是否已改变
        private bool _isFileChanged = false;
        //是否已分析过依赖
        private bool _isAnalyzed = false;

        /// <summary>
        /// 我要依赖的项
        /// </summary>
        private HashSet<AssetTarget> _dependencies = new HashSet<AssetTarget>();
        /// <summary>
        /// 依赖我的项
        /// </summary>
        private HashSet<AssetTarget> _dependsChildren = new HashSet<AssetTarget>();

        public AssetTarget(Object o, FileInfo file, string assetPath)
        {
            this.asset = o;
            this.file = file;
            this.assetPath = assetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                this.file = new FileInfo(AssetBundleUtils.AssetPath + "/mark");
                this.type = AssetType.Builtin;
            }

            string bn = assetPath.Replace(AssetBundleUtils.AssetBundlesPath, "")
                .Replace(AssetBundleUtils.AssetPath, "")
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace(" ", "_");
            bundleName = bn + ".ab";
            //bundleName = bn.ToLower();
            bundleSavePath = string.Format("{0}/StreamingAssets/AssetBundles/{1}",
                        Application.dataPath,
                        bundleName);

            _isFileChanged = true;
        }


        /// <summary>
        /// 分析引用关系
        /// </summary>
        public void Analyze()
        {
            if (_isAnalyzed) return;
            _isAnalyzed = true;

            string oldHash = AssetBundleUtils.GetOldHash(assetPath);
            _isFileChanged = oldHash == null || !oldHash.Equals(GetHash());
            if (_isFileChanged && oldHash != null)
                Debug.Log("file is changed : " + assetPath);

            Object[] deps = EditorUtility.CollectDependencies(new Object[] { asset });
            //提取 resource.builtin
            for (int i = 0; i < deps.Length; i++)
            {
                Object dep = deps[i];
                string path = AssetDatabase.GetAssetPath(dep);
                if (path.StartsWith("Resources"))
                {
                    AssetTarget builtinAsset = AssetBundleUtils.Load(dep);
                    this.AddDepend(builtinAsset);
                    builtinAsset.Analyze();
                }
            }
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

                this.AddDepend(target);

                target.Analyze();
            }
        }

        public void Merge()
        {
            if (this.NeedExportStandalone())
            {
                var children = new List<AssetTarget>(_dependsChildren);
                this.RemoveDependsChildren();
                foreach (AssetTarget child in children)
                {
                    child.AddDepend(this);
                }
            }
        }

        private void GetRoot(HashSet<AssetTarget> rootSet)
        {
            switch (this.exportType)
            {
                case ExportType.Standalone:
                case ExportType.Root:
                    rootSet.Add(this);
                    break;
                default:
                    foreach (AssetTarget item in _dependsChildren)
                    {
                        item.GetRoot(rootSet);
                    }
                    break;
            }
        }

        private bool beforeExportProcess;

        /// <summary>
        /// 在导出之前执行
        /// </summary>
        public void BeforeExport()
        {
            if (beforeExportProcess) return;
            beforeExportProcess = true;

            foreach (AssetTarget item in _dependsChildren)
            {
                item.BeforeExport();
            }

            if (this.exportType == ExportType.Asset)
            {
                HashSet<AssetTarget> rootSet = new HashSet<AssetTarget>();
                this.GetRoot(rootSet);
                if (rootSet.Count > 1)
                    this.exportType = ExportType.Standalone;
            }
        }

        public void UpdateLevel(int level, List<AssetTarget> lvList)
        {
            this.level = level;
            if (level == -1 && levelList != null)
                levelList.Remove(this);
            this.levelList = lvList;
        }

        /// <summary>
        /// 获取所有依赖项
        /// </summary>
        /// <param name="list"></param>
        public void GetDependencies(HashSet<AssetTarget> list)
        {
            var ie = _dependencies.GetEnumerator();
            while (ie.MoveNext())
            {
                AssetTarget target = ie.Current;
                if (target.needSelfExport)
                {
                    list.Add(target);
                }
                else
                {
                    target.GetDependencies(list);
                }
            }
        }

        public List<AssetTarget> dependencies
        {
            get { return new List<AssetTarget>(_dependencies); }
        }

        public bool needRebuild
        {
            get
            {
                if (_isFileChanged)
                    return true;

                foreach (AssetTarget child in _dependsChildren)
                {
                    if (child.needRebuild)
                        return true;
                }

                return false;
            }
        }

        public bool needSelfExport
        {
            get
            {
                if (type == AssetType.Builtin)
                    return false;

                bool v = exportType == ExportType.Root || exportType == ExportType.Standalone;

                return v;
            }
        }

        public bool needExport
        {
            get
            {
                if (isExported || type == AssetType.Builtin)
                    return false;

                bool v = exportType == ExportType.Root || exportType == ExportType.Standalone;

                v &= needRebuild;

                return v;
            }
        }

        /// <summary>
        /// (作为AssetType.Asset时)是否需要单独导出
        /// </summary>
        /// <returns></returns>
        private bool NeedExportStandalone()
        {
            return _dependsChildren.Count > 1;
        }

        /// <summary>
        /// 增加依赖项
        /// </summary>
        /// <param name="target"></param>
        private void AddDepend(AssetTarget target)
        {
            if (target == this || this.ContainsDepend(target))
                return;

            _dependencies.Add(target);
            target.AddDependChild(this);
            this.ClearParentDepend(target);
        }

        /// <summary>
        /// 是否已经包含了这个依赖（检查子子孙孙）
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        private bool ContainsDepend(AssetTarget target, bool recursive = true)
        {
            if (_dependencies.Contains(target))
                return true;
            if (recursive)
            {
                var e = _dependencies.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.ContainsDepend(target, true))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddDependChild(AssetTarget parent)
        {
            _dependsChildren.Add(parent);
        }

        /// <summary>
        /// 我依赖了这个项，那么依赖我的项不需要直接依赖这个项了
        /// </summary>
        private void ClearParentDepend(AssetTarget target = null)
        {
            IEnumerable<AssetTarget> cols = _dependencies;
            if (target != null) cols = new AssetTarget[] { target };
            foreach (AssetTarget at in cols)
            {
                var e = _dependsChildren.GetEnumerator();
                while (e.MoveNext())
                {
                    AssetTarget dc = e.Current;
                    dc.RemoveDepend(at);
                }
            }
        }

        /// <summary>
        /// 移除依赖项
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recursive"></param>
        private void RemoveDepend(AssetTarget target, bool recursive = true)
        {
            _dependencies.Remove(target);
            target._dependsChildren.Remove(this);

            //recursive
            var e = _dependsChildren.GetEnumerator();
            while (e.MoveNext())
            {
                AssetTarget dc = e.Current;
                dc.RemoveDepend(target);
            }
        }

        private void RemoveDependsChildren()
        {
            var all = new List<AssetTarget>(_dependsChildren);
            _dependsChildren.Clear();
            foreach (AssetTarget child in all)
            {
                child._dependencies.Remove(this);
            }
        }

        /// <summary>
        /// 依赖我的项
        /// </summary>
        public List<AssetTarget> dependsChildren
        {
            get { return new List<AssetTarget>(_dependsChildren); }
        }

        int System.IComparable<AssetTarget>.CompareTo(AssetTarget other)
        {
            return other._dependsChildren.Count.CompareTo(_dependsChildren.Count);
        }

        public string GetHash()
        {
            if (type == AssetType.Builtin)
                return "0000000000";
            else
                return AssetBundleUtils.GetFileHash(file.FullName);
        }

        public void WriteBundle(BuildAssetBundleOptions options)
        {
            Debug.Log("Export : >>>>> " + this.assetPath);

            this.isExported = true;

            var children = dependencies;

            Object[] assets = new Object[children.Count + 1];
            assets[0] = asset;

            for (int i = 0; i < children.Count; i++)
            {
                var item = children[i];
                assets[i + 1] = children[i].asset;
            }

            string savePath = bundleSavePath;

            if (file.Extension.EndsWith("unity"))
            {
                BuildPipeline.BuildStreamedSceneAssetBundle(
                    new string[] { file.FullName },
                    savePath,
                    EditorUserBuildSettings.activeBuildTarget,
                    BuildOptions.UncompressedAssetBundle);
            }
            else
            {
                BuildPipeline.BuildAssetBundle(
                    asset,
                    assets,
                    savePath,
                    options,
                    EditorUserBuildSettings.activeBuildTarget);
            }
        }

        public void WriteDependInfo(StreamWriter sw)
        {
            HashSet<AssetTarget> deps = new HashSet<AssetTarget>();
            this.GetDependencies(deps);

            sw.WriteLine(string.Format("{0}.info", bundleName));
            //写入依赖信息
            sw.WriteLine(string.Format("{0}", deps.Count));
            //File Name
            sw.WriteLine(file.Name);

            foreach (AssetTarget item in deps)
            {
                sw.WriteLine(item.bundleName);
            }
        }

        public void WriteDependInfo()
        {
            string path = string.Format("{0}.info", bundleSavePath);
            FileInfo fi = new FileInfo(path);
            if (fi.Exists)
                fi.Delete();
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs);
            this.WriteDependInfo(sw);
            sw.Flush();
            sw.Close();
            fs.Close();
        }

        public void WriteCache(StreamWriter sw)
        {
            sw.WriteLine(this.assetPath);
            sw.WriteLine(GetHash());
        }
    }
}
