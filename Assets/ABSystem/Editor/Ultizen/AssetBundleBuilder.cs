using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Uzen.AB;

public class AssetBundleBuilder
{
    [MenuItem("Ultizen/Build AssetBundles")]
    public static void BuildAssetBundles()
    {
        AssetBundleUtils.Init();
        AssetBundleBuilder builder = new AssetBundleBuilder();

        builder.BuildScenes();
        builder.BuildCharacters();

        builder.Export();
        AssetBundleUtils.ClearCache();
        AssetDatabase.SaveAssets();
    }


    static BuildAssetBundleOptions options =
        BuildAssetBundleOptions.DeterministicAssetBundle |
        BuildAssetBundleOptions.CollectDependencies |
        BuildAssetBundleOptions.UncompressedAssetBundle |
        BuildAssetBundleOptions.CompleteAssets;

    private int version = 1;
    private List<AssetTarget> newBuildTargets = new List<AssetTarget>();

    void BuildScenes()
    {
        DirectoryInfo bundleDir = new DirectoryInfo(AssetBundleUtils.AssetBundlesPath + "/Scences");
        string[] parttern = new string[] { "*.prefab", "*.exr", "*.unity" };
        string[] excludes = new string[] { "_PathPoint.prefab" };

        Func<FileInfo, bool> act = (f) =>
        {
            for (int i = 0; i < excludes.Length; i++)
            {
                if (f.Name.EndsWith(excludes[i]))
                    return false;
            }
            return true;
        };

        for (int i = 0; i < parttern.Length; i++)
        {
            FileInfo[] prefabs = bundleDir.GetFiles(parttern[i], SearchOption.AllDirectories);
            var list = from s in prefabs
                       where act(s)
                       select s;

            foreach (FileInfo file in list)
            {
                AssetTarget target = AssetBundleUtils.Load(file);
                target.type = AssetType.BattleScene;
                target.exportType = ExportType.Root;
            }
        }
    }

    void BuildCharacters()
    {
        DirectoryInfo bundleDir = new DirectoryInfo(AssetBundleUtils.AssetBundlesPath + "/Characters");
        string[] parttern = new string[] { "*.prefab", "*.controller" };
        for (int i = 0; i < parttern.Length; i++)
        {
            FileInfo[] prefabs = bundleDir.GetFiles(parttern[i], SearchOption.AllDirectories);
            foreach (FileInfo file in prefabs)
            {
                AssetTarget target = AssetBundleUtils.Load(file);
                target.type = AssetType.Asset;
                target.exportType = ExportType.Root;
            }
        }
    }

    public void Export()
    {
        var all = AssetBundleUtils.GetAll();
        foreach (AssetTarget target in all)
        {
            target.Analyze();
        }
        all = AssetBundleUtils.GetAll();
        foreach (AssetTarget target in all)
        {
            target.Merge();
        }
        all = AssetBundleUtils.GetAll();
        foreach (AssetTarget target in all)
        {
            target.BeforeExport();
        }

        //Build Export Tree
        all = AssetBundleUtils.GetAll();
        List<List<AssetTarget>> tree = new List<List<AssetTarget>>();
        foreach (AssetTarget target in all)
        {
            BuildExportTree(target, tree, 0);
        }

        //Export
        this.Export(tree, 0);
        this.SaveDepAll(all);
        this.RemoveUnused(all);
        //打包Zip
        //this.Compress();

        AssetBundleUtils.SaveCache();
    }

    void BuildExportTree(AssetTarget parent, List<List<AssetTarget>> tree, int currentLevel)
    {
        if (parent.level == -1 && parent.type != AssetType.Builtin)
        {
            List<AssetTarget> levelList = null;
            if (tree.Count > currentLevel)
            {
                levelList = tree[currentLevel];
            }
            else
            {
                levelList = new List<AssetTarget>();
                tree.Add(levelList);
            }
            levelList.Add(parent);
            parent.UpdateLevel(currentLevel + 1, levelList);

            foreach (AssetTarget ei in parent.dependsChildren)
            {
                if (ei.level != -1 && ei.level <= parent.level)
                {
                    ei.UpdateLevel(-1, null);
                }
                BuildExportTree(ei, tree, currentLevel + 1);
            }
        }
    }

    void Export(List<List<AssetTarget>> tree, int currentLevel)
    {
        if (currentLevel >= tree.Count)
            return;

        BuildPipeline.PushAssetDependencies();
        List<AssetTarget> levelList = tree[currentLevel];

        //把Child个数多的放在前面
        levelList.Sort();

        foreach (AssetTarget ei in levelList)
        {
            Export(ei, ei.needExport);
        }
        if (currentLevel < tree.Count)
        {
            Export(tree, currentLevel + 1);
        }
        BuildPipeline.PopAssetDependencies();
    }

    void Export(AssetTarget target, bool force)
    {
        if (target.needExport || force)
        {
            //写入 .assetbundle 包
            target.WriteBundle(options);

            newBuildTargets.Add(target);
        }
    }

    void SaveDepAll(List<AssetTarget> all)
    {
        string path = string.Format("{0}/StreamingAssets/AssetBundles/dep.all", Application.dataPath);
        
        if (File.Exists(path))
            File.Delete(path);
        FileStream fs = new FileStream(path, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = 0; i < all.Count; i++)
        {
            AssetTarget target = all[i];
            if (target.needSelfExport)
                target.WriteDependInfo(sw);
        }
        sw.Close();
        fs.Close();
    }

    /// <summary>
    /// 删除未使用的AB，可能是上次打包出来的，而这一次没生成的
    /// </summary>
    /// <param name="all"></param>
    void RemoveUnused(List<AssetTarget> all)
    {
        HashSet<string> usedSet = new HashSet<string>();
        for (int i = 0; i < all.Count; i++)
        {
            AssetTarget target = all[i];
            if (target.needSelfExport)
                usedSet.Add(target.bundleName);
        }

        string bundleSavePath = string.Format("{0}/StreamingAssets/AssetBundles", Application.dataPath);
        DirectoryInfo di = new DirectoryInfo(bundleSavePath);
        FileInfo[] abFiles = di.GetFiles("*.ab");
        for (int i = 0; i < abFiles.Length; i++)
        {
            FileInfo fi = abFiles[i];
            if (usedSet.Add(fi.Name))
            {
                fi.Delete();
            }
        }
    }
}