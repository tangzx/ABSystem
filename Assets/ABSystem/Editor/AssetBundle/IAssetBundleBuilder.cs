using System.Collections.Generic;

namespace Tangzx.ABSystem
{
    public interface IAssetBundleBuilder
    {
        AssetBundleEntry createFakeEntry();

        List<AssetBundleEntry> GetAll();
    }
}
