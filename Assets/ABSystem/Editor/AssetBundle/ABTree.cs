using System.Collections.Generic;

namespace Tangzx.ABSystem
{
    public class DependencyTreeNode
    {
        /// <summary>
        /// 我要依赖的项
        /// </summary>
        private HashSet<DependencyTreeNode> _dependParentSet = new HashSet<DependencyTreeNode>();
        /// <summary>
        /// 依赖我的项
        /// </summary>
        private HashSet<DependencyTreeNode> _dependChildrenSet = new HashSet<DependencyTreeNode>();

        /// <summary>
        /// 增加依赖项
        /// </summary>
        /// <param name="target"></param>
        public void AddDependParent(DependencyTreeNode target)
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
        public bool ContainsDepend(DependencyTreeNode target, bool recursive = true)
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

        public void AddDependChild(DependencyTreeNode parent)
        {
            _dependChildrenSet.Add(parent);
        }

        /// <summary>
        /// 我依赖了这个项，那么依赖我的项不需要直接依赖这个项了
        /// </summary>
        public void ClearParentDepend(DependencyTreeNode target = null)
        {
            IEnumerable<DependencyTreeNode> cols = _dependParentSet;
            if (target != null) cols = new DependencyTreeNode[] { target };
            foreach (DependencyTreeNode at in cols)
            {
                var e = _dependChildrenSet.GetEnumerator();
                while (e.MoveNext())
                {
                    DependencyTreeNode dc = e.Current;
                    dc.RemoveDependParent(at);
                }
            }
        }

        /// <summary>
        /// 移除依赖项
        /// </summary>
        /// <param name="target"></param>
        /// <param name="recursive"></param>
        public void RemoveDependParent(DependencyTreeNode target, bool recursive = true)
        {
            _dependParentSet.Remove(target);
            target._dependChildrenSet.Remove(this);

            //recursive
            var dcc = new HashSet<DependencyTreeNode>(_dependChildrenSet);
            var e = dcc.GetEnumerator();
            while (e.MoveNext())
            {
                DependencyTreeNode dc = e.Current;
                dc.RemoveDependParent(target);
            }
        }

        public void RemoveDependChildren()
        {
            var all = new List<DependencyTreeNode>(_dependChildrenSet);
            _dependChildrenSet.Clear();
            foreach (DependencyTreeNode child in all)
            {
                child._dependParentSet.Remove(this);
            }
        }

        public delegate bool EachHandler(DependencyTreeNode node);

        public void EachChildNode(EachHandler processor)
        {
            foreach (DependencyTreeNode child in _dependChildrenSet)
            {
                if (!processor(child))
                {
                    break;
                }
            }
        }

        public DependencyTreeNode[] GetChildren()
        {
            DependencyTreeNode[] children = new DependencyTreeNode[_dependChildrenSet.Count];
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
}