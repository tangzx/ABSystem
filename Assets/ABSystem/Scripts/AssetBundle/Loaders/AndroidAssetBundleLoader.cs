using System.Collections;
using UnityEngine;
using Tangzx.ABSystem;

class AndroidAssetBundleLoader : MobileAssetBundleLoader
{
    protected override IEnumerator LoadFromBuiltin()
    {
#if UNITY_5_3 || UNITY_5_4
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