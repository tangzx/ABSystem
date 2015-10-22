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

    public virtual void Dispose()
    {
        Debug.Log("Unload : " + path);

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

        if (onUnloaded != null)
            onUnloaded(this);
    }

    public bool isReady
    {
        get { return _isReady; }

        set { _isReady = value; }
    }

    public virtual Object mainObject
    {
        get
        {
            if (_mainObject == null && _isReady)
            {
                _mainObject = bundle.mainAsset;
            }
            return _mainObject;
        }
    }
}