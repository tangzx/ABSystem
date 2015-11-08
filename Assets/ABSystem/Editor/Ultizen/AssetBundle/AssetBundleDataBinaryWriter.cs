using System.Collections.Generic;
using System.IO;
using Uzen.AB;

public class AssetBundleDataBinaryWriter : AssetBundleDataWriter
{
    public override void Save(Stream stream, AssetTarget[] targets)
    {
        BinaryWriter sw = new BinaryWriter(stream);
        //写入文件头判断文件类型用，ABDB 意思即 Asset-Bundle-Data-Binary
        sw.Write(new char[] { 'A', 'B', 'D', 'B' });

        for (int i = 0; i < targets.Length; i++)
        {
            AssetTarget target = targets[i];
            HashSet<AssetTarget> deps = new HashSet<AssetTarget>();
            target.GetDependencies(deps);

            //bundle name
            sw.Write(target.bundleName);
            //File Name
            sw.Write(target.file.Name);
            //hash
            sw.Write(target.bundleCrc);
            //type
            sw.Write((int)target.compositeType);
            //写入依赖信息
            sw.Write(deps.Count);

            foreach (AssetTarget item in deps)
            {
                sw.Write(item.bundleName);
            }
        }
        sw.Close();
    }
}