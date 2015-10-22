using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Uzen.AB
{
    public enum LoadState
    {
        State_None = 0,
        State_Loading = 1,
        State_Error = 2,
        State_Complete = 3
    }

    public class AssetBundleManager : MonoBehaviour
    {
        public static AssetBundleManager Instance;
        public static string NAME = "AssetBundleManager";

        public delegate void LoadAssetCompleteHandler(AssetBundleInfo info);
        public delegate void LoaderCompleteHandler(AssetBundleLoader info);
        public delegate void LoadProgressHandler(AssetBundleLoadProgress progress);

        /// <summary>
        /// 同时最大的加载数
        /// </summary>
        private const int MAX_REQUEST = 3;
        /// <summary>
        /// 可再次申请的加载数
        /// </summary>
        private int _requestRemain = MAX_REQUEST;
        /// <summary>
        /// 当前申请要加载的队列
        /// </summary>
        private Queue<AssetBundleLoader> _requestQueue = new Queue<AssetBundleLoader>();

        /// <summary>
        /// 加载队列
        /// </summary>
        private List<AssetBundleLoader> _currentLoadQueue = new List<AssetBundleLoader>();
        /// <summary>
        /// 未完成的
        /// </summary>
        private HashSet<AssetBundleLoader> _nonCompleteLoaderSet = new HashSet<AssetBundleLoader>();
        /// <summary>
        /// 已加载完成的缓存列表
        /// </summary>
        private Dictionary<string, AssetBundleInfo> _loadedAssetBundle = new Dictionary<string, AssetBundleInfo>();
        /// <summary>
        /// 已创建的所有Loader列表(包括加载完成和未完成的)
        /// </summary>
        private Dictionary<string, AssetBundleLoader> _loaderCache = new Dictionary<string, AssetBundleLoader>();
        /// <summary>
        /// 当前是否还在加载，如果加载，则暂时不回收
        /// </summary>
        private bool _isCurrentLoading;

        private AssetBundleLoadProgress _progress = new AssetBundleLoadProgress();
        /// <summary>
        /// 进度
        /// </summary>
        public LoadProgressHandler onProgress;

        public AssetBundlePathResolver pathResolver;

        private AssetBundleDataReader depInfoReader;

        public AssetBundleManager()
        {
            Instance = this;
            pathResolver = new AssetBundlePathResolver();
        }

        protected void Awake()
        {
			InvokeRepeating("CheckUnusedBundle", 0, 5);
            LoadDepInfo();
        }

        void LoadDepInfo()
        {
            depInfoReader = new AssetBundleDataReader();
            string depFile = string.Format("{0}/{1}", pathResolver.BundleCacheDir, pathResolver.DependFileName);
            string srcFile = pathResolver.GetBundleSourceFile(pathResolver.DependFileName);

            if (File.Exists(depFile))
            {
                FileStream fs = new FileStream(depFile, FileMode.Open);
                depInfoReader.Read(fs);
                fs.Close();
            }
            else
            {
                Debug.LogError(string.Format("{0} not exist!", depFile));
            }
        }

        void OnDestroy()
        {
            this.RemoveAll();
        }

        /// <summary>
        /// 通过ShortName获取FullName
        /// </summary>
        /// <param name="shortFileName"></param>
        /// <returns></returns>
        public string GetAssetBundleFullName(string shortFileName)
        {
            return depInfoReader.GetFullName(shortFileName);
        }

        public AssetBundleLoader Load(string path, LoadAssetCompleteHandler handler = null)
        {
            AssetBundleLoader loader = this.CreateLoader(path);
            
            if (loader.isComplete)
            {
                if (handler != null)
                    handler(loader.bundleInfo);
            }
            else
            {
                if (handler != null)
                    loader.onComplete += handler;
                _isCurrentLoading = true;

                if (loader.state < LoadState.State_Loading)
                    _nonCompleteLoaderSet.Add(loader);
                this.StartLoad();
            }
            return loader;
        }

        internal AssetBundleLoader CreateLoader(string path)
        {
            AssetBundleLoader loader = null;

            if (_loaderCache.ContainsKey(path))
            {
                loader = _loaderCache[path];
            }
            else
            {
                loader = new AssetBundleLoader();
                loader.bundleManager = this;
                loader.bundleData = depInfoReader.GetAssetBundleInfo(path);
                loader.path = loader.bundleData.fullName;
                loader.bundleName = loader.bundleData.fullName;
                _loaderCache[path] = loader;
            }

            return loader;
        }

        void StartLoad()
        {
            if (_nonCompleteLoaderSet.Count > 0)
            {
                List<AssetBundleLoader> loaders = new List<AssetBundleLoader>(_nonCompleteLoaderSet);
                for (int i = 0; i < loaders.Count; i++)
                {
                    this.CheckNonCompleteDependLoaders(loaders[i]);
                }
                loaders = new List<AssetBundleLoader>(_nonCompleteLoaderSet);
                _nonCompleteLoaderSet.Clear();

                var e = loaders.GetEnumerator();
                while (e.MoveNext())
                {
                    _currentLoadQueue.Add(e.Current);
                }

                _progress = new AssetBundleLoadProgress();
                _progress.total = _currentLoadQueue.Count;

                e = loaders.GetEnumerator();
                while (e.MoveNext())
                {
                    e.Current.Load();
                }
            }
        }

        void CheckNonCompleteDependLoaders(AssetBundleLoader loader)
        {
            /*
            for (int i = 0; i < loader.depLoaders.Count; i++)
            {
                AssetBundleLoader depLoader = loader.depLoaders[i];
                if (depLoader.state < LoadState.State_Loading && _nonCompleteLoaderSet.Contains(depLoader) == false)
                {
                    _nonCompleteLoaderSet.Add(depLoader);
                }
            }
            */
        }
        
        public void RemoveAll()
        {
            this.StopAllCoroutines();

            _currentLoadQueue.Clear();
            _requestQueue.Clear();
            foreach (AssetBundleInfo abi in _loadedAssetBundle.Values)
            {
                abi.Dispose();
            }
            _loadedAssetBundle.Clear();
            _loaderCache.Clear();
        }

        public AssetBundleInfo GetBundleInfo(string key)
        {
            var e = _loadedAssetBundle.GetEnumerator();
            while (e.MoveNext())
            {
                AssetBundleInfo abi = e.Current.Value;
                if (abi.bundleName == key)
                    return abi;
            }
            return null;
        }

        /// <summary>
        /// 请求加载Bundle，这里统一分配加载时机，防止加载太卡
        /// </summary>
        /// <param name="loader"></param>
        internal void RequestLoadBundle(AssetBundleLoader loader)
        {
            if (_requestRemain < 0) _requestRemain = 0;

            if (_requestRemain == 0)
            {
                _requestQueue.Enqueue(loader);
            }
            else
            {
                this.LoadBundle(loader);
            }
        }

        void CheckRequestList()
        {
            while (_requestRemain > 0 && _requestQueue.Count > 0)
            {
                AssetBundleLoader loader = _requestQueue.Dequeue();
                this.LoadBundle(loader);
            }
        }

        void LoadBundle(AssetBundleLoader loader)
        {
            if (!loader.isComplete)
            {
                loader.LoadBundle();
                _requestRemain--;
            }
        }

        internal void LoadError(AssetBundleLoader loader)
        {
            this.LoadComplete(loader);
        }

        internal void LoadComplete(AssetBundleLoader loader)
        {
            _requestRemain++;
            _currentLoadQueue.Remove(loader);

            if (onProgress != null)
            {
                _progress.loader = loader;
                _progress.complete = _progress.total - _currentLoadQueue.Count;
                this.onProgress(_progress);
            }

            if (_currentLoadQueue.Count == 0 && _nonCompleteLoaderSet.Count == 0)
            {
                _isCurrentLoading = false;
            }
            else
            {
                this.CheckRequestList();
            }
        }

        internal AssetBundleInfo CreateBundleInfo(AssetBundleLoader loader, AssetBundle assetBundle)
        {
            AssetBundleInfo abi = new AssetBundleInfo();
            abi.bundleName = loader.bundleName;
            abi.path = loader.path;
            abi.bundle = assetBundle;
            abi.loader = loader;

            _loadedAssetBundle[loader.bundleName] = abi;
            return abi;
        }

        internal void RemoveBundleInfo(AssetBundleInfo abi)
        {
            abi.Dispose();
            _loadedAssetBundle.Remove(abi.bundleName);
        }

        /// <summary>
        /// 当前是否在加载状态
        /// </summary>
        public bool isCurrentLoading { get { return _isCurrentLoading; } }

		void CheckUnusedBundle()
		{
			this.UnloadUnusedBundle();
		}

        /// <summary>
        /// 卸载不用的
        /// </summary>
        public void UnloadUnusedBundle(bool force = false)
        {
            if (_isCurrentLoading == false || force)
            {
                List<string> keys = new List<string>(_loadedAssetBundle.Keys);
                bool hasUnusedBundle = false;
                //一次最多卸载的个数，防止卸载过多太卡
                int unloadLimit = 20;
                int unloadCount = 0;

                do
                {
                    hasUnusedBundle = false;
                    for (int i = 0; i < keys.Count && !_isCurrentLoading && unloadCount < unloadLimit; i++)
                    {
                        if (_isCurrentLoading && !force)
                            break;

                        string key = keys[i];
                        AssetBundleInfo abi = _loadedAssetBundle[key];
                        if (abi.IsUnused())
                        {
                            hasUnusedBundle = true;
                            unloadCount++;

                            this.RemoveBundleInfo(abi);

                            i--;
                            keys.Remove(key);
                        }
                    }
                } while (hasUnusedBundle && !_isCurrentLoading && unloadCount < unloadLimit);
#if UNITY_EDITOR
                if (unloadCount > 0)
                {
                    Debug.Log("===>> Unload Count: " + unloadCount);
                }
#endif
            }
        }

        public void RemoveBundle(string key)
        {
            AssetBundleInfo abi = this.GetBundleInfo(key);
            if (abi != null)
            {
                this.RemoveBundleInfo(abi);
            }
        }

        /// <summary>
        /// 删除Bundle跟踪器
        /// 警告：删除跟踪器可能会使原来的AssetBundle被清除
        /// </summary>
        /// <param name="go"></param>
        public void RemoveBundleTracker(GameObject go)
        {
            AssetBundleTrack abt = go.GetComponent<AssetBundleTrack>();
            if (abt != null)
                Destroy(abt);
        }

#if UNITY_EDITOR
        public List<AssetBundleInfo> GetAllAssetBundleInfos()
        {
            return new List<AssetBundleInfo>(_loadedAssetBundle.Values);
        }
#endif
    }
}
