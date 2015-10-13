using System;
using System.Collections.Generic;
using System.IO;

public class AssetBundleData
{
    public string shortName;
    public string fullName;
    public string hash;
    public string[] dependencies;
    public bool isAnalyzed;
    public AssetBundleData[] dependList;
}

class AssetBundleDataReader
{
    public Dictionary<string, AssetBundleData> infoMap = new Dictionary<string, AssetBundleData>();

    private Dictionary<string, string> shortName2FullName = new Dictionary<string, string>();

    public void Read(Stream fs)
    {
        StreamReader sr = new StreamReader(fs);
        while (true)
        {
            string name = sr.ReadLine();
            if (string.IsNullOrEmpty(name))
                break;
            
            //去除 .info
            string hash = sr.ReadLine();
            int depsCount = Convert.ToInt32(sr.ReadLine());
            string[] deps = new string[depsCount];
            string shortFileName = sr.ReadLine();
            if (!shortName2FullName.ContainsKey(shortFileName))
                shortName2FullName.Add(shortFileName, name);
            for (int i = 0; i < depsCount; i++)
            {
                deps[i] = sr.ReadLine();
            }

            AssetBundleData info = new AssetBundleData();
            info.hash = hash;
            info.fullName = name;
            info.shortName = shortFileName;
            info.dependencies = deps;
            infoMap[name] = info;
        }
        sr.Close();
        fs.Close();
    }

    /// <summary>
    /// 分析生成依赖树
    /// </summary>
    public void Analyze()
    {
        var e = infoMap.GetEnumerator();
        while (e.MoveNext())
        {
            Analyze(e.Current.Value);
        }
    }

    void Analyze(AssetBundleData abd)
    {
        if (!abd.isAnalyzed)
        {
            abd.isAnalyzed = true;
            abd.dependList = new AssetBundleData[abd.dependencies.Length];
            for (int i = 0; i < abd.dependencies.Length; i++)
            {
                AssetBundleData dep = this.GetAssetBundleInfo(abd.dependencies[i]);
                abd.dependList[i] = dep;
                this.Analyze(dep);
            }
        }
    }

    public string GetFullName(string shortName)
    {
        string fullName = null;
        shortName2FullName.TryGetValue(shortName, out fullName);
        return fullName;
    }

    public AssetBundleData GetAssetBundleInfoByShortName(string shortName)
    {
        string fullName = GetFullName(shortName);
        if (fullName != null && infoMap.ContainsKey(fullName))
            return infoMap[fullName];
        return null;
    }

    public AssetBundleData GetAssetBundleInfo(string fullName)
    {
        if (fullName != null && infoMap.ContainsKey(fullName))
            return infoMap[fullName];
        return null;
    }
}