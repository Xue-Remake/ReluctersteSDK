using ReluctersteSDK.DataStructures.Interfaces;

namespace ReluctersteSDK.DataStructures.Structures
{
    /// <summary>
    /// 基础图拓扑节点抽象基类，实现纯粹的连接维护与双向同步
    /// </summary>
    public abstract class XNodeBase : IXNode
    {
        // 底层使用 List<IXNode> 维护连接
        protected readonly List<IXNode> _inputs = new();
        protected readonly List<IXNode> _outputs = new();

        /// <summary>当前节点的入边/输入节点集合（只读视图）</summary>
        public IReadOnlyList<IXNode> Inputs => _inputs;

        /// <summary>当前节点指向的出边/输出节点集合（只读视图）</summary>
        public IReadOnlyList<IXNode> Outputs => _outputs;

        /// <summary>
        /// 校验是否允许建立连接，子类可根据业务重写扩展约束
        /// </summary>
        /// <param name="target">待校验的目标下游节点</param>
        /// <returns>若允许建立连接则返回 true；否则返回 false</returns>
        public virtual bool CanConnectTo(IXNode target)
        {
            if (target == null) return false;
            if (ReferenceEquals(this, target)) return false; // 默认禁止自环
            if (_outputs.Contains(target)) return false;      // 禁止重复连接

            return true;
        }

        /// <summary>
        /// 当前节点作为输入源，连接到 target
        /// this -> target
        /// </summary>
        /// <param name="target">目标下游节点</param>
        /// <returns>若连接成功建立则返回 true；否则返回 false</returns>
        public bool ConnectTo(IXNode target)
        {
            if (!CanConnectTo(target))
                return false;

            if (target is XNodeBase targetBase)
            {
                _outputs.Add(target);
                targetBase._inputs.Add(this);
                OnConnected(target);
                targetBase.OnReceivedConnection(this);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 断开与目标节点的连接
        /// this -x-> target
        /// </summary>
        /// <param name="target">目标下游节点</param>
        /// <returns>若成功断开连接则返回 true；若连接不存在或断开失败则返回 false</returns>
        public bool DisconnectFrom(IXNode target)
        {
            if (target == null || !_outputs.Contains(target))
                return false;

            if (target is XNodeBase targetBase)
            {
                _outputs.Remove(target);
                targetBase._inputs.Remove(this);
                OnDisconnected(target);
                targetBase.OnLostConnection(this);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 彻底切断当前节点在网络中的所有双向连接
        /// </summary>
        public virtual void ClearConnections()
        {
            // 断开所有的输出连接 (this -> target)
            for (int i = _outputs.Count - 1; i >= 0; i--)
            {
                DisconnectFrom(_outputs[i]);
            }

            // 断开所有的输入连接 (source -> this)
            for (int i = _inputs.Count - 1; i >= 0; i--)
            {
                if (_inputs[i] is XNodeBase sourceBase)
                {
                    sourceBase.DisconnectFrom(this);
                }
            }
        }

        #region 生命周期钩子（供派生类扩展具体业务逻辑）

        /// <summary>
        /// 当主动连接到下游节点时触发
        /// </summary>
        /// <param name="target">新建立连接的目标下游节点</param>
        protected virtual void OnConnected(IXNode target) { }

        /// <summary>
        /// 当被上游节点连接时触发
        /// </summary>
        /// <param name="source">新建立连接的源上游节点</param>
        protected virtual void OnReceivedConnection(IXNode source) { }

        /// <summary>
        /// 当主动断开与下游节点的连接时触发
        /// </summary>
        /// <param name="target">已被断开的目标下游节点</param>
        protected virtual void OnDisconnected(IXNode target) { }

        /// <summary>
        /// 当失去上游节点的连接时触发
        /// </summary>
        /// <param name="source">已被断开的源上游节点</param>
        protected virtual void OnLostConnection(IXNode source) { }

        #endregion
    }
}
