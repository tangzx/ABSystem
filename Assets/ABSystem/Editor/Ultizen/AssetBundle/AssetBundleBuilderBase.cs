using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Uzen.AB;

public abstract class AssetBundleBuilderBase
{
    static BuildAssetBundleOptions options =
        BuildAssetBundleOptions.DeterministicAssetBundle |
        BuildAssetBundleOptions.CollectDependencies |
        BuildAssetBundleOptions.UncompressedAssetBundle |
        BuildAssetBundleOptions.CompleteAssets;

    /// <summary>
    /// 本次增量更新的
    /// </summary>
    protected List<AssetTarget> newBuildTargets = new List<AssetTarget>();

    protected AssetBundlePathResolver pathResolver;

    public AssetBundleBuilderBase(AssetBundlePathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
        this.InitDirs();
        AssetBundleUtils.pathResolver = pathResolver;
    }

    void InitDirs()
    {
        new DirectoryInfo(pathResolver.BundleSavePath).Create();
        new FileInfo(pathResolver.HashCacheSaveFile).Directory.Create();
    }

    public void Begin()
    {
        EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
        AssetBundleUtils.Init();
    }

    public void AddRootTargets(DirectoryInfo bundleDir, string[] partterns = null, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (partterns == null)
            partterns = new string[] { "*.*" };
        for (int i = 0; i < partterns.Length; i++)
        {
            FileInfo[] prefabs = bundleDir.GetFiles(partterns[i], searchOption);
            foreach (FileInfo file in prefabs)
            {
                AssetTarget target = AssetBundleUtils.Load(file);
                target.exportType = ExportType.Root;
            }
        }
    }

    public void End()
    {
        AssetBundleUtils.ClearCache();
        EditorUtility.ClearProgressBar();
    }

    public void Export()
    {
        try
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
                target.AnalyzeIfDepTreeChanged();
                BuildExportTree(target, tree, 0);
            }

            //Export
            this.Export(tree, 0);
            this.SaveDepAll(all);
            this.RemoveUnused(all);

            AssetBundleUtils.SaveCache();
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
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
            Export(ei);
        }
        if (currentLevel < tree.Count)
        {
            Export(tree, currentLevel + 1);
        }
        BuildPipeline.PopAssetDependencies();
    }

    void Export(AssetTarget target)
    {
        if (target.needExport)
        {
            //写入 .assetbundle 包
            target.WriteBundle(options);

            if (target.isNewBuild)
                newBuildTargets.Add(target);
        }
    }

    void SaveDepAll(List<AssetTarget> all)
    {
        string path = Path.Combine(pathResolver.BundleSavePath, pathResolver.DependFileName);

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

        DirectoryInfo di = new DirectoryInfo(pathResolver.BundleSavePath);
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