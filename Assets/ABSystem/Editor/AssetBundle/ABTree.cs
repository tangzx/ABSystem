using System.Collections.Generic;
using System.IO;

namespace Tangzx.ABSystem
{
    public abstract class DependencyTreeNode<T>
    {
        public T userData;
        /// <summary>
        /// 我要依赖的项
        /// </summary>
        private HashSet<DependencyTreeNode<T>> _dependParentSet = new HashSet<DependencyTreeNode<T>>();
        /// <summary>
        /// 依赖我的项
        /// </summary>
        private HashSet<DependencyTreeNode<T>> _dependChildrenSet = new HashSet<DependencyTreeNode<T>>();

        /// <summary>
        /// 增加依赖项
        /// </summary>
        /// <param name="target"></param>
        public void AddDependParent(DependencyTreeNode<T> target)
        {
            if (target == this || this.ContainsDepend(target))
                return;

            _dependParentSet.Add(target);
            target.AddDependChild(this);
            this.ClearParentDepend(target);
        }

        /// <summary>
        /// 是否已经包含了这个依赖（检查子子孙孙）
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public bool ContainsDepend(DependencyTreeNode<T> target, bool recursive = true)
        {
            if (_dependParentSet.Contains(target))
                return true;
            if (recursive)
            {
                var e = _dependParentSet.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.ContainsDepend(target, true))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void AddDependChild(DependencyTreeNode<T> parent)
        {
            _dependChildrenSet.Add(parent);
        }

        /// <summary>
        /// 我依赖了这个项，那么依赖我的项不需要直接依赖这个项了
        /// </summary>
        public void ClearParentDepend(DependencyTreeNode<T> target = null)
        {
            IEnumerable<DependencyTreeNode<T>> cols = _dependParentSet;
            if (target != null) cols = new DependencyTreeNode<T>[] { target };
            foreach (DependencyTreeNode<T> at in cols)
            {
                var e = _dependChildrenSet.GetEnumerator();
                while (e.MoveNext())
                {
                    DependencyTreeNode<T> dc = e.Current;
                    dc.RemoveDependParent(at);
                }
            }
        }

        /// <summary>
        /// 移除依赖项
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recursive"></param>
        public void RemoveDependParent(DependencyTreeNode<T> target, bool recursive = true)
        {
            _dependParentSet.Remove(target);
            target._dependChildrenSet.Remove(this);

            //recursive
            var dcc = new HashSet<DependencyTreeNode<T>>(_dependChildrenSet);
            var e = dcc.GetEnumerator();
            while (e.MoveNext())
            {
                DependencyTreeNode<T> dc = e.Current;
                dc.RemoveDependParent(target);
            }
        }

        public void RemoveDependChildren()
        {
            var all = new List<DependencyTreeNode<T>>(_dependChildrenSet);
            _dependChildrenSet.Clear();
            foreach (DependencyTreeNode<T> child in all)
            {
                child._dependParentSet.Remove(this);
            }
        }

        public delegate bool EachHandler(DependencyTreeNode<T> node, T userData);

        public void EachChildNode(EachHandler processor)
        {
            foreach (DependencyTreeNode<T> child in _dependChildrenSet)
            {
                if (!processor(child, child.userData))
                {
                    break;
                }
            }
        }

        public void EachParentNode(EachHandler processor)
        {
            foreach (DependencyTreeNode<T> child in _dependParentSet)
            {
                if (!processor(child, child.userData))
                {
                    break;
                }
            }
        }

        public DependencyTreeNode<T>[] GetChildren()
        {
            DependencyTreeNode<T>[] children = new DependencyTreeNode<T>[_dependChildrenSet.Count];
            var e = _dependChildrenSet.GetEnumerator();
            int idx = 0;
            while (e.MoveNext())
            {
                children[idx++] = e.Current;
            }
            return children;
        }

        public int childCount
        {
            get
            {
                return _dependChildrenSet.Count;
            }
        }
    }

    public class AssetBundleEntry : DependencyTreeNode<AssetBundleEntry>
    {
        /// <summary>
        /// 保存地址
        /// </summary>
        public string bundleSavePath
        {
            get
            {
                return Path.Combine(AssetBundleUtils.pathResolver.BundleSavePath, bundleName);
            }
        }
        /// <summary>
        /// BundleName
        /// </summary>
        public string bundleName;
        /// <summary>
        /// 短名
        /// </summary>
        public string bundleShortName;
        /// <summary>
        /// 相对于Assets文件夹的目录
        /// </summary>
        public string assetPath;
        /// <summary>
        /// 导出类型
        /// </summary>
        public AssetBundleExportType exportType = AssetBundleExportType.Asset;

        public AssetBundleEntry()
        {
            userData = this;
        }

        private void GetRoot(HashSet<AssetBundleEntry> rootSet)
        {
            switch (this.exportType)
            {
                case AssetBundleExportType.Standalone:
                case AssetBundleExportType.Root:
                    rootSet.Add(this);
                    break;
                default:
                    EachChildNode((node, entry) => {
                        entry.GetRoot(rootSet);
                        return true;
                    });
                    break;
            }
        }

        private bool beforeExportProcessed;

        /// <summary>
        /// 在导出之前执行
        /// </summary>
        public void BeforeExport()
        {
            if (beforeExportProcessed) return;
            beforeExportProcessed = true;

            EachChildNode((node, entry) =>
            {
                entry.BeforeExport();
                return true;
            });

            if (exportType == AssetBundleExportType.Asset)
            {
                HashSet<AssetBundleEntry> rootSet = new HashSet<AssetBundleEntry>();
                this.GetRoot(rootSet);
                if (rootSet.Count > 1)
                    this.exportType = AssetBundleExportType.Standalone;
            }
        }

        public virtual void Merge()
        {
            if (this.NeedExportStandalone())
            {
                var children = GetChildren();
                this.RemoveDependChildren();
                foreach (var child in children)
                {
                    child.AddDependParent(this);
                }
            }
        }

        /// <summary>
        /// (作为AssetType.Asset时)是否需要单独导出
        /// </summary>
        /// <returns></returns>
        private bool NeedExportStandalone()
        {
            return childCount > 1;
        }

        /// <summary>
        /// 获取所有依赖项
        /// </summary>
        /// <param name="list"></param>
        public void GetDependencies(HashSet<AssetBundleEntry> list)
        {
            EachParentNode((node, entry) =>
            {
                if (entry.needSelfExport)
                {
                    list.Add(entry);
                }
                else
                {
                    entry.GetDependencies(list);
                }
                return true;
            });
        }

        /// <summary>
        /// 是不是自身的原因需要导出如指定的类型prefab等，有些情况下是因为依赖树原因需要单独导出
        /// </summary>
        public bool needSelfExport
        {
            get
            {
                bool v = exportType == AssetBundleExportType.Root || exportType == AssetBundleExportType.Standalone;
                return v;
            }
        }

        /// <summary>
        /// 是不是需要重编
        /// </summary>
        public virtual bool needRebuild
        {
            get
            {
                bool rebuild = false;
                EachChildNode((node, entry) =>
                {
                    if (entry.needRebuild)
                    {
                        rebuild = true;
                        return false;
                    }
                    return true;
                });
                return rebuild;
            }
        }

        public AssetBundleExportType compositeType
        {
            get
            {
                AssetBundleExportType type = exportType;
                if (type == AssetBundleExportType.Root && childCount > 0)
                    type |= AssetBundleExportType.Asset;
                return type;
            }
        }

        public virtual string bundleCrc
        {
            get; set;
        }

        public virtual string[] GetAssetNames()
        {
            return new string[] { assetPath };
        }
    }

    public class AssetBundlePack : AssetBundleEntry
    {
        List<AssetTarget> assets = new List<AssetTarget>();

        public AssetBundlePack()
        {
            exportType = AssetBundleExportType.Standalone;
        }

        public void Add(AssetTarget at)
        {
            at.exportType = AssetBundleExportType.Asset;
            assets.Add(at);

            var children = at.GetChildren();
            at.RemoveDependChildren();
            foreach (var child in children)
            {
                child.AddDependParent(this);
            }
        }

        public override string[] GetAssetNames()
        {
            List<string> s = new List<string>();
            for (int i = 0; i < assets.Count; i++)
            {
                s.AddRange(assets[i].GetAssetNames());
            }
            return s.ToArray();
        }
    }
}