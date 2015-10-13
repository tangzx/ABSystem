using System.IO;
using UnityEditor;

public class AssetBundleBuilder : AssetBundleBuilderBase
{
    public AssetBundleBuilder() : base(new AssetBundlePathResolver())
    {

    }

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