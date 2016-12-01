using System.Collections.Generic;
using System.IO;

namespace Tangzx.ABSystem
{
    public class AssetBundleDataBinaryWriter : AssetBundleDataWriter
    {
        public override void Save(Stream stream, AssetTarget[] targets)
        {
            BinaryWriter sw = new BinaryWriter(stream);
            //写入文件头判断文件类型用，ABDB 意思即 Asset-Bundle-Data-Binary
            sw.Write(new char[] { 'A', 'B', 'D', 'B' });

            List<string> bundleNames = new List<string>();

            for (int i = 0; i < targets.Length; i++)
            {
                AssetTarget target = targets[i];
                bundleNames.Add(target.bundleName);
            }

            //写入文件名池
            sw.Write(bundleNames.Count);
            for (int i = 0; i < bundleNames.Count; i++)
            {
                sw.Write(bundleNames[i]);
            }

            //写入详细信息
            for (int i = 0; i < targets.Length; i++)
            {
                AssetTarget target = targets[i];
                HashSet<AssetTarget> deps = new HashSet<AssetTarget>();
                target.GetDependencies(deps);

                //debug name
                sw.Write(target.assetPath);
                //bundle name
                sw.Write(bundleNames.IndexOf(target.bundleName));
                //File Name
                sw.Write(target.bundleShortName);
                //hash
                sw.Write(target.bundleCrc);
                //type
                sw.Write((int)target.compositeType);
                //写入依赖信息
                sw.Write(deps.Count);

                foreach (AssetTarget item in deps)
                {
                    sw.Write(bundleNames.IndexOf(item.bundleName));
                }
            }
            sw.Close();
        }
    }
}