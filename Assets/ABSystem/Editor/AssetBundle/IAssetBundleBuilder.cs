using System.Collections.Generic;

namespace Tangzx.ABSystem
{
    public interface IAssetBundleBuilder
    {
        AssetBundlePack createFakeEntry();

        List<AssetBundleEntry> GetAll();
    }
}
