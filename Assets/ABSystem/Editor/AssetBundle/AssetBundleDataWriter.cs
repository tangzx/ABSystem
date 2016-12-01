using System.Collections.Generic;
using System.IO;

namespace Tangzx.ABSystem
{
    public class AssetBundleDataWriter
    {
        public void Save(string path, AssetTarget[] targets)
        {
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            Save(fs, targets);
        }

        public virtual void Save(Stream stream, AssetTarget[] targets)
        {
            StreamWriter sw = new StreamWriter(stream);
            //写入文件头判断文件类型用，ABDT 意思即 Asset-Bundle-Data-Text
            sw.WriteLine("ABDT");

            for (int i = 0; i < targets.Length; i++)
            {
                AssetTarget target = targets[i];
                HashSet<AssetTarget> deps = new HashSet<AssetTarget>();
                target.GetDependencies(deps);

                //debug name
                sw.WriteLine(target.assetPath);
                //bundle name
                sw.WriteLine(target.bundleName);
                //File Name
                sw.WriteLine(target.bundleShortName);
                //hash
                sw.WriteLine(target.bundleCrc);
                //type
                sw.WriteLine((int)target.compositeType);
                //写入依赖信息
                sw.WriteLine(deps.Count);

                foreach (AssetTarget item in deps)
                {
                    sw.WriteLine(item.bundleName);
                }
                sw.WriteLine("<------------->");
            }
            sw.Close();
        }
    }
}