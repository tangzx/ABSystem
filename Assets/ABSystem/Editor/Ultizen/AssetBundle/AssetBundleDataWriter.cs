using System.Collections.Generic;
using System.IO;
using Uzen.AB;

public class AssetBundleDataWriter
{
    public void Save(string path, AssetTarget[] targets)
    {
        Save(new FileStream(path, FileMode.CreateNew), targets);
    }

    public virtual void Save(Stream stream, AssetTarget[] targets)
    {
        StreamWriter sw = new StreamWriter(stream);

        for (int i = 0; i < targets.Length; i++)
        {
            AssetTarget target = targets[i];
            HashSet<AssetTarget> deps = new HashSet<AssetTarget>();
            target.GetDependencies(deps);

            sw.WriteLine(target.bundleName);
            //hash
            sw.WriteLine(target.bundleCrc);
            //写入依赖信息
            sw.WriteLine(string.Format("{0}", deps.Count));
            //File Name
            sw.WriteLine(target.file.Name);

            foreach (AssetTarget item in deps)
            {
                sw.WriteLine(item.bundleName);
            }
        }
        sw.Close();
    }
}