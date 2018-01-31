using System;
using System.IO;

namespace Tangzx.ABSystem
{
    public interface IAssetBundleManager
    {
        AssetBundlePathResolver pathResolver { get; set; }
        void Init(Action callback);
        void Init(Stream depStream, Action callback);
        AssetBundleLoader Load(string path, AssetBundleManager.LoadAssetCompleteHandler handler = null);
        AssetBundleLoader Load(string path, int prority, AssetBundleManager.LoadAssetCompleteHandler handler = null);
        AssetBundleInfo GetBundleInfo(string key);
        void RemoveBundle(string key);
        void RemoveAll();
    }
}