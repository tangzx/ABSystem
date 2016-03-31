using System.Collections.Generic;
using UnityEngine;

public class AssetBundleBuildConfig : ScriptableObject
{
	public enum Format
	{
		Text,
		Bin
	}

	public Format depInfoFileFormat = Format.Bin;

    public List<AssetBundleFilter> filters = new List<AssetBundleFilter>();
}

[System.Serializable]
public class AssetBundleFilter
{
    public bool valid = true;
    public string path = string.Empty;
    public string filter = "*.prefab";
}