namespace Tangzx.ABSystem
{
    /// <summary>
    /// 对现有结果进行修正
    /// </summary>
    public interface IAssetBundleEntryModifier
    {
        void process(IAssetBundleBuilder builder, BuildPhase phase);
    }
}
