using System.Collections.Generic;
using UnityEngine;
using Uzen.AB;

public class AssetBundleInfo
{
    public delegate void OnUnloadedHandler(AssetBundleInfo abi);
    public OnUnloadedHandler onUnloaded;

    internal AssetBundle bundle;

    public string bundleName;
    public string path;
    public AssetBundleLoader loader;

    private bool _isReady;

    private Object _mainObject;

    [SerializeField]
    public int refCount { get; private set; }

    private HashSet<AssetBundleInfo> deps = new HashSet<AssetBundleInfo>();
    private List<string> depChildren = new List<string>();

    public AssetBundleInfo()
    {

    }

    public void AddDependency(AssetBundleInfo target)
    {
        if (deps.Add(target))
        {
            target.Retain();
            target.depChildren.Add(this.bundleName);
        }
    }

    public void Retain()
    {
        refCount++;
    }

    public void Release()
    {
        refCount--;
    }

    public bool IsUnused()
    {
        return _isReady && refCount < 0;
    }

    public void Dispose()
    {
        Debug.Log("Unload : " + path);
#if !AB_MODE && UNITY_EDITOR

#else
        if (bundle != null)
            bundle.Unload(false);

        var e = deps.GetEnumerator();
        while (e.MoveNext())
        {
            AssetBundleInfo dep = e.Current;
            dep.depChildren.Remove(this.bundleName);
            dep.Release();
        }
        deps.Clear();
#endif
        if (onUnloaded != null)
            onUnloaded(this);
    }

    public bool isReady
    {
        get { return _isReady; }

        set { _isReady = value; }
    }

    public Object mainObject
    {
        get
        {
            if (_mainObject == null && _isReady)
            {
#if !AB_MODE && UNITY_EDITOR
                string newPath = AssetBundlePathResolver.instance.GetEditorModePath(path);
                _mainObject = UnityEditor.AssetDatabase.LoadMainAssetAtPath(newPath);
#else
                _mainObject = bundle.mainAsset;
#endif
            }
            return _mainObject;
        }
    }
}