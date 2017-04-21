using System.Collections.Generic;

namespace Tangzx.ABSystem
{
    public interface IAssetBundleBuilder
    {
        AssetBundlePack createFakeEntry(string assetPath);

        List<AssetBundleEntry> GetAll();
    }
}
