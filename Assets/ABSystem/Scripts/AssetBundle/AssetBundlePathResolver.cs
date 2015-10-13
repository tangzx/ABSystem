/// <summary>
/// AB 打包及运行时路径解决器
/// </summary>
public class AssetBundlePathResolver
{
    public static AssetBundlePathResolver instance;

    public AssetBundlePathResolver()
    {
        instance = this;
    }

#if UNITY_EDITOR
    /// <summary>
    /// AB 保存的路径
    /// </summary>
    public virtual string BundleSaveDir { get { return "Assets/StreamingAssets/AssetBundles/"; } }
    /// <summary>
    /// AB打包的原文件HashCode要保存到的路径，下次可供增量打包
    /// </summary>
    public virtual string HashCacheSaveFile { get { return "Assets/AssetBundles/cache.txt"; } }
    /// <summary>
    /// 在编辑器模型下将 abName 转为 Assets/... 路径
    /// 这样就可以不用打包直接用了
    /// </summary>
    /// <param name="abName"></param>
    /// <returns></returns>
    public virtual string GetEditorModePath(string abName)
    {
        //将 Assets.AA.BB.prefab 转为 Assets/AA/BB.prefab
        string p = abName.Substring(0, abName.Length - 3);//去除最后的 .ab
        p = p.Replace(".", "/");
        int last = p.LastIndexOf("/");
        
        if (last == -1)
            return p;

        string path = string.Format("{0}.{1}", p.Substring(0, last), p.Substring(last + 1));
        return path;
    }
#endif
    /// <summary>
    /// AB 依赖信息文件名
    /// </summary>
    public virtual string DependFileName { get { return "dep.all"; } }
    /// <summary>
    /// 运行时AB缓存的路径
    /// </summary>
    public virtual string BundleCacheDir { get { return BundleSaveDir; } }
}