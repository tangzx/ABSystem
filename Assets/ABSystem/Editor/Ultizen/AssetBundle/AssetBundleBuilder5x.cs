using UnityEditor;
using UnityEngine;
using Uzen.AB;

public class AssetBundleBuilder5x : ABBuilder
{
    public AssetBundleBuilder5x(AssetBundlePathResolver resolver)
        : base(resolver)
    {

    }

    public override void Export()
    {
        base.Export();

        var all = AssetBundleUtils.GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            AssetTarget target = all[i];
            AssetImporter importer = AssetImporter.GetAtPath(target.assetPath);
            if (importer)
            {
                if (target.needSelfExport)
                {
                    importer.assetBundleName = target.bundleName;
                }
                else
                {
                    importer.assetBundleName = null;
                }
            }
        }

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(this.pathResolver.BundleSavePath, BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);
        this.SaveDepAll(all);
        //this.RemoveUnused(all);
    }
}
