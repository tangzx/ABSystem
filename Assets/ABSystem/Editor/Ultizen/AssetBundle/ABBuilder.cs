using Uzen.AB;
using UnityEditor;
using System.IO;

public class ABBuilder
{

    protected AssetBundlePathResolver pathResolver;

    public ABBuilder() : this(new AssetBundlePathResolver())
    {

    }

    public ABBuilder(AssetBundlePathResolver resolver)
    {
        this.pathResolver = resolver;
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

    public void End()
    {
        AssetBundleUtils.SaveCache();
        AssetBundleUtils.ClearCache();
        EditorUtility.ClearProgressBar();
    }

    public virtual void Analyze()
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
    }

    public virtual void Export()
    {
        this.Analyze();
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
}
