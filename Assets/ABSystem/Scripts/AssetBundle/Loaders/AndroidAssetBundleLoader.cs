using System.Collections;
using Tangzx.ABSystem;
using UnityEngine;

class AndroidAssetBundleLoader : MobileAssetBundleLoader
{
    protected override IEnumerator LoadFromBuiltin()
    {
#if UNITY_5_3 || UNITY_5_4
        //直接用 LoadFromFile
        _assetBundleSourceFile = bundleManager.pathResolver.GetBundleSourceFile(bundleName, false);
        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(_assetBundleSourceFile);
        yield return req;
        _bundle = req.assetBundle;
#else
        _bundle = AssetBundle.CreateFromFile(_assetBundleSourceFile);
        yield return null;
#endif
        Complete();
    }
}