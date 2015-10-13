using System.Collections;
using System.IO;
using UnityEngine;

namespace Uzen.AB
{
    /// <summary>
    /// 这个不释放
    /// </summary>
    public class AssetBundleLoader
    {
        internal AssetBundleManager.LoadAssetCompleteHandler onComplete;

        public string path;
        public string bundleName;
        public AssetBundleData bundleData;
        public AssetBundleInfo bundleInfo;
        public AssetBundleManager bundleManager;
        public LoadState state = LoadState.State_None;
        public AssetBundleLoader[] depLoaders;
        
        private int _currentLoadingDepCount;
        private AssetBundle _bundle;
        private bool _hasError;
        private string _assetBundleFile;
        private string _assetBundleURL;

        /// <summary>
        /// 开始加载
        /// </summary>
        public void Load()
        {
            if (_hasError)
                state = LoadState.State_Error;

            if (state == LoadState.State_None)
            {
                state = LoadState.State_Loading;
#if !AB_MODE && UNITY_EDITOR
                if (bundleInfo == null)
                {
                    this.state = LoadState.State_Complete;
                    this.bundleInfo = bundleManager.CreateBundleInfo(this, null);
                    this.bundleInfo.isReady = true;
                    this.bundleInfo.onUnloaded = OnBundleUnload;
                }

                bundleManager.StartCoroutine(this.LoadResource());
#else
                this.LoadDepends();
#endif
            }
            else if (state == LoadState.State_Error)
            {
                this.Error();
            }
            else if (state == LoadState.State_Complete)
            {
                this.Complete();
            }
        }

#if !AB_MODE && UNITY_EDITOR
        IEnumerator LoadResource()
        {
            yield return new WaitForSeconds(0.1f);
            this.Complete();
        }
#endif

        void LoadDepends()
        {
            if (depLoaders == null)
            {
                depLoaders = new AssetBundleLoader[bundleData.dependencies.Length];
                for (int i = 0; i < bundleData.dependencies.Length; i++)
                {
                    depLoaders[i] = bundleManager.CreateLoader(bundleData.dependencies[i]);
                }
            }

            _currentLoadingDepCount = 0;
            for (int i = 0; i < depLoaders.Length; i++)
            {
                AssetBundleLoader depLoader = depLoaders[i];
                if (depLoader.state != LoadState.State_Error && depLoader.state != LoadState.State_Complete)
                {
                    _currentLoadingDepCount++;
                    depLoader.onComplete += OnDepComplete;
                    depLoader.Load();
                }
            }
            this.CheckDepComplete();
        }

        /// <summary>
        /// 其它都准备好了，加载AssetBundle
        /// 注意：这个方法只能被 AssetBundleManager 调用
        /// 由 Manager 统一分配加载时机，防止加载过卡
        /// </summary>
        public void LoadBundle()
        {
            _assetBundleFile = string.Format("{0}/{1}", bundleManager.pathResolver.BundleCacheDir, bundleName);
            _assetBundleURL = bundleManager.pathResolver.GetBundleSourceFile(bundleName);

            if (File.Exists(_assetBundleFile))
                bundleManager.StartCoroutine(LoadFromFile());
            else
                bundleManager.StartCoroutine(LoadFromBuiltin());
        }

        IEnumerator LoadFromFile()
        {
            if (state != LoadState.State_Error)
            {
                _bundle = AssetBundle.CreateFromFile(_assetBundleFile);
                yield return null;

                //byte[] bytes = File.ReadAllBytes(_assetBundleFile);
                //AssetBundleCreateRequest req = AssetBundle.CreateFromMemory(bytes);
                //yield return req;
                //_bundle = req.assetBundle;

                this.Complete();
            }
        }

        IEnumerator LoadFromBuiltin()
        {
            if (state != LoadState.State_Error)
            {
                //加载主体
                WWW www = new WWW(_assetBundleURL);
                yield return www;

                File.WriteAllBytes(_assetBundleFile, www.bytes);

                _bundle = www.assetBundle;

                www.Dispose();
                www = null;

                this.Complete();
            }
        }

        void OnDepComplete(AssetBundleInfo abi)
        {
            _currentLoadingDepCount--;
            this.CheckDepComplete();
        }

        void CheckDepComplete()
        {
            if (_currentLoadingDepCount == 0)
            {
                bundleManager.RequestLoadBundle(this);
            }
        }

        void Complete()
        {
            if (bundleInfo == null)
            {
                this.state = LoadState.State_Complete;

                this.bundleInfo = bundleManager.CreateBundleInfo(this, _bundle);
                this.bundleInfo.isReady = true;
                this.bundleInfo.onUnloaded = OnBundleUnload;
                foreach (AssetBundleLoader depLoader in depLoaders)
                {
                    bundleInfo.AddDependency(depLoader.bundleInfo);
                }

                _bundle = null;
            }
            if (onComplete != null)
            {
                var handler = onComplete;
                onComplete = null;
                handler(bundleInfo);
            }
            bundleManager.LoadComplete(this);
        }

        private void OnBundleUnload(AssetBundleInfo abi)
        {
            this.bundleInfo = null;
            this.state = LoadState.State_None;
        }

        public void Error()
        {
            _hasError = true;
            this.state = LoadState.State_Error;
            this.bundleInfo = null;

            if (onComplete != null)
            {
                var handler = onComplete;
                onComplete = null;
                handler(bundleInfo);
            }
            bundleManager.LoadError(this);
        }

        public bool isComplete
        {
            get
            {
                return state == LoadState.State_Error || state == LoadState.State_Complete;
            }
        }
    }
}
