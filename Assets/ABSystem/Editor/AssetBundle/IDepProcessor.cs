namespace Tangzx.ABSystem
{
    /// <summary>
    /// 当保存 dep.all 文件时被调用
    /// </summary>
    public interface IDepProcessor
    {
        void PostProcess(AssetBundleEntry[] bundleEntries);
    }
}