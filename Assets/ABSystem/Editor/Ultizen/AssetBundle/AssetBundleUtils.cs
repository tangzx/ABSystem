using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tangzx.ABSystem
{
    class AssetCacheInfo
    {
        /// <summary>
        /// 源文件的hash，比较变化
        /// </summary>
        public string fileHash;
        /// <summary>
        /// 源文件meta文件的hash，部分类型的素材需要结合这个来判断变化
        /// 如：Texture
        /// </summary>
        public string metaHash;
        /// <summary>
        /// 上次打好的AB的CRC值，用于增量判断
        /// </summary>
        public string bundleCrc;
        /// <summary>
        /// 所依赖的那些文件
        /// </summary>
        public string[] depNames;
    }

    class AssetBundleUtils
    {
        public static AssetBundlePathResolver pathResolver;
        public static DirectoryInfo AssetDir = new DirectoryInfo(Application.dataPath);
        public static string AssetPath = AssetDir.FullName;
        public static DirectoryInfo ProjectDir = AssetDir.Parent;
        public static string ProjectPath = ProjectDir.FullName;

        static Dictionary<int, AssetTarget> _object2target;
        static Dictionary<string, AssetTarget> _assetPath2target;
        static Dictionary<string, string> _fileHashCache;
        static Dictionary<string, AssetCacheInfo> _fileHashOld;

        public static void Init()
        {
            _object2target = new Dictionary<int, AssetTarget>();
            _assetPath2target = new Dictionary<string, AssetTarget>();
            _fileHashCache = new Dictionary<string, string>();
            _fileHashOld = new Dictionary<string, AssetCacheInfo>();
            LoadCache();
        }

        public static void ClearCache()
        {
            _object2target = null;
            _assetPath2target = null;
            _fileHashCache = null;
            _fileHashOld = null;
        }

        public static void LoadCache()
        {
            string cacheTxtFilePath = pathResolver.HashCacheSaveFile;
            if (File.Exists(cacheTxtFilePath))
            {
                string value = File.ReadAllText(cacheTxtFilePath);
                StringReader sr = new StringReader(value);

                //版本比较
                string vString = sr.ReadLine();
                bool wrongVer = false;
                try
                {
                    Version ver = new Version(vString);
                    wrongVer = ver.Minor < AssetBundleManager.version.Minor || ver.Major < AssetBundleManager.version.Major;
                }
                catch (Exception) { wrongVer = true; }

                if (wrongVer)
                    return;

                //读取缓存的信息
                while (true)
                {
                    string path = sr.ReadLine();
                    if (path == null)
                        break;

                    AssetCacheInfo cache = new AssetCacheInfo();
                    cache.fileHash = sr.ReadLine();
                    cache.metaHash = sr.ReadLine();
                    cache.bundleCrc = sr.ReadLine();
                    int depsCount = Convert.ToInt32(sr.ReadLine());
                    cache.depNames = new string[depsCount];
                    for (int i = 0; i < depsCount; i++)
                    {
                        cache.depNames[i] = sr.ReadLine();
                    }

                    _fileHashOld[path] = cache;
                }
            }
        }

        public static void SaveCache()
        {
            StreamWriter sw = new StreamWriter(pathResolver.HashCacheSaveFile);
            sw.WriteLine(AssetBundleManager.version.ToString());
            foreach (AssetTarget target in _object2target.Values)
            {
                target.WriteCache(sw);
            }

            sw.Flush();
            sw.Close();
        }

        public static List<AssetTarget> GetAll()
        {
            return new List<AssetTarget>(_object2target.Values);
        }

        public static AssetTarget Load(Object o)
        {
            AssetTarget target = null;
            if (o != null)
            {
                int instanceId = o.GetInstanceID();

                if (_object2target.ContainsKey(instanceId))
                {
                    target = _object2target[instanceId];
                }
                else
                {
                    string assetPath = AssetDatabase.GetAssetPath(o);
                    string key = assetPath;
                    //Builtin，内置素材，path为空
                    if (string.IsNullOrEmpty(assetPath))
                        key = string.Format("Builtin______{0}", o.name);
                    else
                        key = string.Format("{0}/{1}", assetPath, instanceId);

                    if (_assetPath2target.ContainsKey(key))
                    {
                        target = _assetPath2target[key];
                    }
                    else
                    {
                        if (assetPath.StartsWith("Resources"))
                        {
                            assetPath = string.Format("{0}/{1}.{2}", assetPath, o.name, o.GetType().Name);
                        }
                        FileInfo file = new FileInfo(Path.Combine(ProjectPath, assetPath));
                        target = new AssetTarget(o, file, assetPath);
                        _object2target[instanceId] = target;
                        _assetPath2target[key] = target;
                    }
                }
            }
            return target;
        }

        public static AssetTarget Load(FileInfo file, System.Type t)
        {
            AssetTarget target = null;
            string fullPath = file.FullName;
            int index = fullPath.IndexOf("Assets");
            if (index != -1)
            {
                string assetPath = fullPath.Substring(index);
                if (_assetPath2target.ContainsKey(assetPath))
                {
                    target = _assetPath2target[assetPath];
                }
                else
                {
                    Object o = null;
                    if (t == null)
                        o = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    else
                        o = AssetDatabase.LoadAssetAtPath(assetPath, t);

                    if (o != null)
                    {
                        int instanceId = o.GetInstanceID();

                        if (_object2target.ContainsKey(instanceId))
                        {
                            target = _object2target[instanceId];
                        }
                        else
                        {
                            target = new AssetTarget(o, file, assetPath);
                            string key = string.Format("{0}/{1}", assetPath, instanceId);
                            _assetPath2target[key] = target;
                            _object2target[instanceId] = target;
                        }
                    }
                }
            }

            return target;
        }

        public static AssetTarget Load(FileInfo file)
        {
            return Load(file, null);
        }

        public static string ConvertToABName(string assetPath)
        {
            string bn = assetPath
                .Replace(AssetPath, "")
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace(" ", "_")
                .ToLower();
            return bn;
        }

        public static string GetFileHash(string path, bool force = false)
        {
            string _hexStr = null;
            if (_fileHashCache.ContainsKey(path) && !force)
            {
                _hexStr = _fileHashCache[path];
            }
            else if (File.Exists(path) == false)
            {
                _hexStr = "FileNotExists";
            }
            else
            {
                FileStream fs = new FileStream(path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                _hexStr = HashUtil.Get(fs);
                _fileHashCache[path] = _hexStr;
                fs.Close();
            }
            
            return _hexStr;
        }

        public static AssetCacheInfo GetCacheInfo(string path)
        {
            if (_fileHashOld.ContainsKey(path))
                return _fileHashOld[path];
            return null;
        }
    }
}
