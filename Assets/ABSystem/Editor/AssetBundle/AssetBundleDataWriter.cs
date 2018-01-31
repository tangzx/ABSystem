using System.Collections.Generic;
using System.IO;

namespace Tangzx.ABSystem
{
    public class AssetBundleDataWriter
    {
        public void Save(string path, AssetBundleEntry[] targets)
        {
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            Save(fs, targets);
        }

        public virtual void Save(Stream stream, AssetBundleEntry[] targets)
        {
            StreamWriter sw = new StreamWriter(stream);
            //写入文件头判断文件类型用，ABDT 意思即 Asset-Bundle-Data-Text
            sw.WriteLine("ABDT");

            for (int i = 0; i < targets.Length; i++)
            {
                AssetBundleEntry target = targets[i];
                HashSet<AssetBundleEntry> deps = new HashSet<AssetBundleEntry>();
                target.GetDependencies(deps);

                //debug name
                sw.WriteLine(target.assetPath);
                //bundle name
                sw.WriteLine(target.bundleName);
                //File Name
                sw.WriteLine(target.bundleShortName);
                //hash
                sw.WriteLine(target.BundleCrc);
                //type
                sw.WriteLine((int)target.CompositeType);
                //写入依赖信息
                sw.WriteLine(deps.Count);

                foreach (AssetBundleEntry item in deps)
                {
                    sw.WriteLine(item.bundleName);
                }
                sw.WriteLine("<------------->");
            }
            sw.Close();
        }
    }
}