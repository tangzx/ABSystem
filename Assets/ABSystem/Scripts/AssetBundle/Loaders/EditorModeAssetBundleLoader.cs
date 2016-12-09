#if UNITY_EDITOR

#if AB_MODE
using System.Collections;

namespace Tangzx.ABSystem
{
    /// <summary>
    /// 编辑器模式并启用AB_MODE下用的加载器
    /// 与iOS的相同，直接加载StreamAssets里的AB
    /// </summary>
    public class EditorModeAssetBundleLoader : IOSAssetBundleLoader
    {
        protected override IEnumerator LoadFromPackage()
        {
            _assetBundleSourceFile = AssetBundlePathResolver.instance.GetBundleSourceFile(bundleName, false);
            return base.LoadFromPackage();
        }
    }
}
#else
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Tangzx.ABSystem
{
    /// <summary>
    /// 编辑器模式下用的加载器
    /// </summary>
    public class EditorModeAssetBundleLoader : AssetBundleLoader
    {
        class ABInfo : AssetBundleInfo
        {
            public override Object mainObject
            {
                get
                {
                    string newPath = AssetBundlePathResolver.instance.GetEditorModePath(bundleName);
                    Object mainObject = AssetDatabase.LoadMainAssetAtPath(newPath);
                    return mainObject;
                }
            }
        }

        public override void Start()
        {
            bundleManager.StartCoroutine(this.LoadResource());
        }

        private void OnBundleUnload(AssetBundleInfo abi)
        {
            this.bundleInfo = null;
            this.state = LoadState.State_None;
        }

        IEnumerator LoadResource()
        {
            yield return new WaitForEndOfFrame();

            string newPath = AssetBundlePathResolver.instance.GetEditorModePath(bundleName);
            Object mainObject = AssetDatabase.LoadMainAssetAtPath(newPath);
            if (mainObject)
            {
                if (bundleInfo == null)
                {
                    state = LoadState.State_Complete;
                    bundleInfo = bundleManager.CreateBundleInfo(this, new ABInfo());
                    bundleInfo.isReady = true;
                    bundleInfo.onUnloaded = OnBundleUnload;
                }

                Complete();
            }
            else
            {
                state = LoadState.State_Error;
                Error();
            }
        }
    }
}
#endif

#endif