using System.Collections;
using UnityEngine;

namespace Tangzx.ABSystem
{
    /// <summary>
    /// 注意：未经测试，不要用
    /// </summary>
    class AndroidAssetBundleLoader : MobileAssetBundleLoader
    {
        protected override IEnumerator LoadFromPackage()
        {
            //兼容低版本API
#if UNITY_4 || UNITY_4_6 || UNITY_5_1 || UNITY_5_2
            _bundle = AssetBundle.CreateFromFile(_assetBundleSourceFile);
            yield return null;
#else
            //直接用 LoadFromFile
            _assetBundleSourceFile = bundleManager.pathResolver.GetBundleSourceFile(bundleName, false);
            AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(_assetBundleSourceFile);
            yield return req;
            _bundle = req.assetBundle;
#endif
            Complete();
        }
    }
}