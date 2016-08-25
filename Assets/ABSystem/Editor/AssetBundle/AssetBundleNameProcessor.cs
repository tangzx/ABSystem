/*
using UnityEditor;
using UnityEngine;

namespace Tangzx.ABSystem
{
#if UNITY_5
    /// <summary>
    /// 把所有导入进来的Asset的assetBundleName置空
    /// 在.meta文件中生成类似
    ///     assetBundleName: 
    ///     assetBundleVariant: 
    /// 的数据。
    /// 作用是仿止在打包之后造成 .meta 文件的变动
    /// </summary>
    public class AssetBundleNameProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                Object o = AssetDatabase.LoadMainAssetAtPath(str);
                if (o is MonoScript ||
                    o is DefaultAsset)
                {

                }
                else
                {
                    AssetImporter importer = AssetImporter.GetAtPath(str);
                    importer.SetAssetBundleNameAndVariant("_", "_");
                    importer.SetAssetBundleNameAndVariant(null, null);
                }
            }
        }
    }
#endif
}*/