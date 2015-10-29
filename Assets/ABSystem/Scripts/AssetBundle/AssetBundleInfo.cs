using System;
using System.Collections.Generic;
using UnityEngine;
using Uzen.AB;
using Object = UnityEngine.Object;

public class AssetBundleInfo
{
    public delegate void OnUnloadedHandler(AssetBundleInfo abi);
    public OnUnloadedHandler onUnloaded;

    internal AssetBundle bundle;

    public string bundleName;
    public AssetBundleLoader loader;

    private bool _isReady;

    private Object _mainObject;

    [SerializeField]
    public int refCount { get; private set; }

    private HashSet<AssetBundleInfo> deps = new HashSet<AssetBundleInfo>();
    private List<string> depChildren = new List<string>();
    private List<WeakReference> references = new List<WeakReference>();

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

    /// <summary>
    /// 增加引用
    /// </summary>
    /// <param name="owner">用来计算引用计数，如果所有的引用对象被销毁了，那么AB也将会被销毁</param>
    public void Retain(Object owner)
    {
        if (owner == null)
            throw new Exception("Please set the user!");

        for (int i = 0; i < references.Count; i++)
        {
            if (references[i].Target == owner)
                return;
        }

        WeakReference wr = new WeakReference(owner);
        references.Add(wr);
    }

    /// <summary>
    /// 释放引用
    /// </summary>
    /// <param name="owner"></param>
    public void Release(object owner)
    {
        for (int i = 0; i < references.Count; i++)
        {
            if (references[i].Target == owner)
            {
                references.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// 实例化对象
    /// </summary>
    /// <param name="user">增加引用的对象</param>
    /// <returns></returns>
    public virtual GameObject Instantiate()
    {
        if (mainObject != null)
        {
            //只有GameObject才可以Instantiate
            if (mainObject is GameObject)
            {
                Object inst = Object.Instantiate(mainObject);
                //这里去掉，用Tracker进行跟踪了
                //this.Retain(inst);
                return (GameObject)inst;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取此对象
    /// </summary>
    /// <param name="user">增加引用的对象</param>
    /// <returns></returns>
    public Object Require(Object user)
    {
        this.Retain(user);
        return mainObject;
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    /// <param name="c">增加引用的Component</param>
    /// <param name="autoBindGameObject">如果为true，则增加引用到它的gameObject对象上</param>
    /// <returns></returns>
    public Object Require(Component c, bool autoBindGameObject)
    {
        if (autoBindGameObject && c && c.gameObject)
            return Require(c.gameObject);
        else
            return Require(c);
    }

    int UpdateReference()
    {
        for (int i = 0; i < references.Count; i++)
        {
            Object o = (Object)references[i].Target;
            if (!o)
            {
                references.RemoveAt(i);
                i--;
            }
        }
        return references.Count;
    }

    /// <summary>
    /// 这个资源是否不用了
    /// </summary>
    /// <returns></returns>
    public bool IsUnused()
    {
        return _isReady && refCount <= 0 && this.UpdateReference() == 0;
    }

    public virtual void Dispose()
    {
        Debug.Log("Unload : " + bundleName);

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
        references.Clear();
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
                if (_mainObject is GameObject)
                {
                    GameObject go = (GameObject)_mainObject;
                    GameObjectTracker tracker = go.AddComponent<GameObjectTracker>();
                    tracker.bundleName = this.bundleName;
                }
            }
            return _mainObject;
        }
    }
}

/// <summary>
/// 生命周期追踪器
/// </summary>
class GameObjectTracker : MonoBehaviour
{
    public string bundleName;

    void Awake()
    {
        if (bundleName != null)
        {
            AssetBundleInfo abi = AssetBundleManager.Instance.GetBundleInfo(bundleName);
            if (abi != null)
                abi.Retain(this);
        }
    }

    void OnDestroy()
    {
        if (bundleName != null)
        {
            AssetBundleInfo abi = AssetBundleManager.Instance.GetBundleInfo(bundleName);
            if (abi != null)
                abi.Release(this);
        }
    }
}