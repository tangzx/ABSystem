using System.Collections;
using UnityEngine;
using Tangzx.ABSystem;

class AndroidAssetBundleLoader : MobileAssetBundleLoader
{
    protected override IEnumerator LoadFromBuiltin()
    {
        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(_assetBundleSourceFile);
        yield return req;
        _bundle = req.assetBundle;

        Complete();
    }
}