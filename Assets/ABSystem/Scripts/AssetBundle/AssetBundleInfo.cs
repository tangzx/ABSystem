using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tangzx.ABSystem
{
    public class AssetBundleInfo
    {
        public delegate void OnUnloadedHandler(AssetBundleInfo abi);
        public OnUnloadedHandler onUnloaded;

        internal AssetBundle bundle;

        public string bundleName;
        public AssetBundleData data;

        /// <summary>
        /// 如果没有其它东西引用的情况下，此AB最小生存时间（单位秒）
        /// 否则有可能刚加载完成就被释放了
        /// </summary>
        public float minLifeTime = 5;

        /// <summary>
        /// 准备完毕时的时间
        /// </summary>
        private float _readyTime;

        /// <summary>
        /// 标记当前是否准备完毕
        /// </summary>
        private bool _isReady;

        private Object _mainObject;

        /// <summary>
        /// 强制的引用计数
        /// </summary>
        [SerializeField]
        public int refCount { get; private set; }

        private HashSet<AssetBundleInfo> deps = HashSetPool<AssetBundleInfo>.Get();
        private List<string> depChildren = ListPool<string>.Get();
        private List<WeakReference> references = ListPool<WeakReference>.Get();

        public AssetBundleInfo()
        {

        }

        public void AddDependency(AssetBundleInfo target)
        {
            if (target != null && deps.Add(target))
            {
                target.Retain();
                target.depChildren.Add(this.bundleName);
            }
        }

        public void ResetLifeTime()
        {
            if (_isReady)
            {
                _readyTime = Time.time;
            }
        }

        /// <summary>
        /// 引用计数增一
        /// </summary>
        public void Retain()
        {
            refCount++;
        }

        /// <summary>
        /// 引用计数减一
        /// </summary>
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
                if (owner.Equals(references[i].Target))
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
            return Instantiate(true);
        }

        public virtual GameObject Instantiate(bool enable)
        {
            if (mainObject != null)
            {
                //只有GameObject才可以Instantiate
                if (mainObject is GameObject)
                {
                    GameObject prefab = mainObject as GameObject;
                    prefab.SetActive(enable);
                    Object inst = Object.Instantiate(prefab);
                    inst.name = prefab.name;
                    Retain(inst);
                    return (GameObject)inst;
                }
            }
            return null;
        }

        public virtual GameObject Instantiate(Vector3 position, Quaternion rotation, bool enable = true)
        {
            if (mainObject != null)
            {
                //只有GameObject才可以Instantiate
                if (mainObject is GameObject)
                {
                    GameObject prefab = mainObject as GameObject;
                    prefab.SetActive(enable);
                    Object inst = Object.Instantiate(prefab, position, rotation);
                    inst.name = prefab.name;
                    Retain(inst);
                    return (GameObject)inst;
                }
            }
            return null;
        }

        public T LoadAsset<T>(Object user, string name) where T : Object
        {
            if (bundle)
            {
                T asset = bundle.LoadAsset<T>(name);
                if (asset)
                    Retain(user);
                return asset;
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

        public T Require<T>(Object user) where T : Object
        {
            if (mainObject is T)
            {
                Retain(user);
                return (T)mainObject;
            }
            return null;
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
        public bool isUnused
        {
            get { return _isReady && refCount <= 0 && UpdateReference() == 0 && Time.time - _readyTime > minLifeTime; }
        }

        public virtual void Dispose()
        {
            UnloadBundle();

            var e = deps.GetEnumerator();
            while (e.MoveNext())
            {
                AssetBundleInfo dep = e.Current;
                if (dep.depChildren != null)
                    dep.depChildren.Remove(this.bundleName);
                dep.Release();
            }
            HashSetPool<AssetBundleInfo>.Release(deps);
            deps = null;
            ListPool<string>.Release(depChildren);
            depChildren = null;
            ListPool<WeakReference>.Release(references);
            references = null;

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
#if UNITY_5
                    string[] names = bundle.GetAllAssetNames();
                    _mainObject = bundle.LoadAsset(names[0]);
#else
                _mainObject = bundle.mainAsset;
#endif
                    //优化：如果是根，则可以 unload(false) 以节省内存
                    if (data.compositeType == AssetBundleExportType.Root)
                        UnloadBundle();
                }
                return _mainObject;
            }
        }

        void UnloadBundle()
        {
            if (bundle != null)
            {
                if (AssetBundleManager.enableLog)
                    Debug.Log("Unload : " + data.compositeType + " >> " + data.debugName);

                bundle.Unload(false);
            }
            bundle = null;
        }
    }
}