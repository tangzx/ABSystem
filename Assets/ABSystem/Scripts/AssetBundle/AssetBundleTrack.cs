using UnityEngine;

namespace Uzen.AB
{
    public class AssetBundleTrack : MonoBehaviour
    {
        static AssetBundleManager abm;

        public string bundleName;

        public AssetBundleInfo bundleInfo;

        void Awake()
        {
            if (abm == null)
            {
                abm = AssetBundleManager.Instance;
            }

            bundleInfo = abm.GetBundleInfo(bundleName);
            if (bundleInfo != null)
            {
                bundleInfo.Retain();
            }
        }

        void OnDestroy()
        {
            if (bundleInfo != null)
            {
                bundleInfo.Release();
                bundleInfo = null;
            }
        }
    }
}
