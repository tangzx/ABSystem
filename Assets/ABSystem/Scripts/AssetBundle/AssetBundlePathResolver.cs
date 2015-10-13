
public class AssetBundlePathResolver
{
    public static AssetBundlePathResolver instance;

    public AssetBundlePathResolver()
    {
        instance = this;
    }

    public virtual string BundleSaveDir { get { return "Assets/StreamingAssets/AssetBundles/"; } }
    public virtual string HashCacheSavePath { get { return "Assets/AssetBundles/cache.txt"; } }
    public virtual string DependFileName { get { return "dep.all"; } }
}