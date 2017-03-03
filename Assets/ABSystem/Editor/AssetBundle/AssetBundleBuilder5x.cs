#if UNITY_5
using System.Collections.Generic;
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

            List<AssetBundleBuild> list = new List<AssetBundleBuild>();
            //标记所有 asset bundle name
            var all = AssetBundleUtils.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = target.bundleName;
                    build.assetNames = new string[] { target.assetPath };
                    list.Add(build);
                }
            }

            //开始打包
            BuildPipeline.BuildAssetBundles(pathResolver.BundleSavePath, list.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

#if UNITY_5_1 || UNITY_5_2
            AssetBundle ab = AssetBundle.CreateFromFile(pathResolver.BundleSavePath + "/AssetBundles");
#else
            AssetBundle ab = AssetBundle.LoadFromFile(pathResolver.BundleSavePath + "/AssetBundles");
#endif
            AssetBundleManifest manifest = ab.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
            //hash
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                {
                    Hash128 hash = manifest.GetAssetBundleHash(target.bundleName);
                    target.bundleCrc = hash.ToString();
                }
            }
            this.SaveDepAll(all);
            ab.Unload(true);
            this.RemoveUnused(all);

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }
    }
}
#endif