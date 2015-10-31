using System.IO;
using UnityEditor;

public class AssetBundleBuilder
{
    [MenuItem("Tang/Build AssetBundles")]
    public static void BuildAssetBundles()
    {
#if UNITY_5
        ABBuilder builder = new AssetBundleBuilder5x(new AssetBundlePathResolver());
#else
        ABBuilder builder = new AssetBundleBuilder4x(new AssetBundlePathResolver());
#endif
        builder.Begin();

        builder.AddRootTargets(new DirectoryInfo("Assets/Prefabs"), new string[] { "*.prefab" });

        builder.Export();
        builder.End();
    }
}