using System.IO;
using UnityEditor;

public class AssetBundleBuilder : AssetBundleBuilderBase
{
    [MenuItem("Tang/Build AssetBundles")]
    public static void BuildAssetBundles()
    {
        AssetBundleBuilder builder = new AssetBundleBuilder();
        builder.Begin();

        builder.AddRootTargets(new DirectoryInfo("Assets/Prefabs"), new string[] { "*.prefab" });

        builder.Export();
        builder.End();
    }
}