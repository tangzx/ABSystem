#if UNITY_5
using UnityEditor;
using UnityEngine;

namespace Tangzx.ABSystem
{
    public class AssetBundleBuilder5x : ABBuilder
    {
        public AssetBundleBuilder5x(AssetBundlePathResolver resolver)
            : base(resolver)
        {

        }

        public override void Export()
        {
            base.Export();

            //标记所有 asset bundle name
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

            //开始打包
            BuildPipeline.BuildAssetBundles(this.pathResolver.BundleSavePath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

#if UNITY_5_3 || UNITY_5_4
            AssetBundle ab = AssetBundle.LoadFromFile(pathResolver.BundleSavePath + "/AssetBundles");
#else
        AssetBundle ab = AssetBundle.CreateFromFile(pathResolver.BundleSavePath + "/AssetBundles");
#endif
            AssetBundleManifest manifest = ab.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
            //清除所有 asset bundle name
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                Hash128 hash = manifest.GetAssetBundleHash(target.bundleName);
                target.bundleCrc = hash.ToString();

                AssetImporter importer = AssetImporter.GetAtPath(target.assetPath);
                if (importer)
                    importer.assetBundleName = null;
            }
            this.SaveDepAll(all);
            ab.Unload(true);
            this.RemoveUnused(all);

            AssetDatabase.Refresh();
        }
    }
}
#endif