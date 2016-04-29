using System.Collections;
using UnityEngine;
using Uzen.AB;

/// <summary>
/// 在IOS下的加载
/// 注意：
/// IOS下加载可以进行优化：直接在raw目录里进行File读取
/// </summary>
public class IOSAssetBundleLoader : MobileAssetBundleLoader
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
        this.Complete();
    }
}
