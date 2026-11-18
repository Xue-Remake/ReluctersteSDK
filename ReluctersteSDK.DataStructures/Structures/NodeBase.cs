using System.Collections.ObjectModel;
using ReluctersteSDK.DataStructures.Interfaces;

namespace ReluctersteSDK.DataStructures.Structures
{
    /// <summary>
    /// 图节点抽象基类：维护双向连接与生命周期钩子，并附带唯一标识与名称。
    /// </summary>
    /// <remarks>
    /// <para>连接是**双向登记**的：<c>A.ConnectTo(B)</c> 会同时把 A 记入 B 的 <see cref="Inputs"/>。</para>
    /// <para>由于需要写入对方的输入列表，**只有 <see cref="NodeBase"/> 之间才能建立连接**；
    /// 传入其它 <see cref="IXNode"/> 实现时 <see cref="CanConnectTo"/> 与 <see cref="ConnectTo"/> 都会返回 false。</para>
    /// <para>钩子（<see cref="OnConnected"/> 等）在拓扑已经一致之后才触发，因此钩子内部可以安全读取连接关系。</para>
    /// </remarks>
    public abstract class NodeBase : INode
    {
        private readonly List<IXNode> _inputs = new List<IXNode>();
        private readonly List<IXNode> _outputs = new List<IXNode>();

        // 只读包装：对外暴露的是视图，调用方无法把 Inputs 强转回 List 后篡改
        private readonly ReadOnlyCollection<IXNode> _inputsView;
        private readonly ReadOnlyCollection<IXNode> _outputsView;

        private string _name;

        /// <inheritdoc />
        public Guid Id { get; }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">试图把名称设为 null。</exception>
        public string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public IReadOnlyList<IXNode> Inputs => _inputsView;

        /// <inheritdoc />
        public IReadOnlyList<IXNode> Outputs => _outputsView;

        /// <summary>获取输入节点数量。</summary>
        public int InputCount => _inputs.Count;

        /// <summary>获取输出节点数量。</summary>
        public int OutputCount => _outputs.Count;

        /// <summary>
        /// 初始化 <see cref="NodeBase"/> 节点实例。
        /// </summary>
        /// <param name="name">节点名称；未指定时使用运行时类型名。</param>
        /// <param name="id">节点唯一标识；未指定时自动生成。</param>
        protected NodeBase(string? name = null, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            _name = name ?? GetType().Name;

            _inputsView = _inputs.AsReadOnly();
            _outputsView = _outputs.AsReadOnly();
        }

        /// <summary>
        /// 校验是否允许建立连接。
        /// </summary>
        /// <param name="target">待校验的目标下游节点。</param>
        /// <returns>允许连接返回 <c>true</c>。</returns>
        /// <remarks>
        /// 默认规则：目标非 null、不是自己、是 <see cref="NodeBase"/>、且尚未连接过。
        /// **默认不禁止成环**——图的语义由使用方决定；需要禁止时重写本方法并调用
        /// <see cref="WouldCreateCycle"/>。
        /// </remarks>
        public virtual bool CanConnectTo(IXNode target)
        {
            if (target is null) return false;
            if (ReferenceEquals(this, target)) return false;
            if (target is not NodeBase) return false;
            if (_outputs.Contains(target)) return false;

            return true;
        }

        /// <summary>把当前节点连接到目标节点（this → target）。</summary>
        /// <param name="target">目标下游节点。</param>
        /// <returns>连接建立成功返回 <c>true</c>；被 <see cref="CanConnectTo"/> 拒绝时返回 <c>false</c>。</returns>
        public bool ConnectTo(IXNode target)
        {
            if (!CanConnectTo(target))
                return false;

            var targetBase = (NodeBase)target;

            // 先把两侧的拓扑都改完，再触发钩子
            _outputs.Add(target);
            targetBase._inputs.Add(this);

            OnConnected(target);
            targetBase.OnReceivedConnection(this);
            return true;
        }

        /// <summary>断开与目标节点的连接（this -x→ target）。</summary>
        /// <param name="target">已连接的目标下游节点。</param>
        /// <returns>确实断开返回 <c>true</c>。</returns>
        public bool DisconnectFrom(IXNode target)
        {
            if (target is null || target is not NodeBase targetBase)
                return false;

            if (!_outputs.Remove(target))
                return false;

            targetBase._inputs.Remove(this);

            OnDisconnected(target);
            targetBase.OnLostConnection(this);
            return true;
        }

        /// <summary>切断当前节点的所有出入连接（双向同步清理）。</summary>
        public virtual void ClearConnections()
        {
            // 倒序遍历，配合删除不会跳过元素
            for (int i = _outputs.Count - 1; i >= 0; i--)
                DisconnectFrom(_outputs[i]);

            for (int i = _inputs.Count - 1; i >= 0; i--)
            {
                if (_inputs[i] is NodeBase source)
                    source.DisconnectFrom(this);
            }
        }

        /// <summary>
        /// 判断把当前节点连接到 <paramref name="target"/> 是否会形成环。
        /// </summary>
        /// <param name="target">候选下游节点。</param>
        /// <returns>会成环返回 <c>true</c>。</returns>
        /// <remarks>
        /// 建立 this → target 会成环，当且仅当 target 已经能沿**输出方向**走到当前节点。
        /// 因此这里从 target 出发沿 Outputs 向前追溯。
        /// </remarks>
        public bool WouldCreateCycle(IXNode target)
        {
            if (target is null) return false;

            var visited = new HashSet<IXNode>();
            var pending = new Stack<IXNode>();
            pending.Push(target);

            while (pending.Count > 0)
            {
                IXNode current = pending.Pop();
                if (ReferenceEquals(current, this))
                    return true;

                if (!visited.Add(current) || current is not NodeBase node)
                    continue;

                for (int i = 0; i < node._outputs.Count; i++)
                    pending.Push(node._outputs[i]);
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{Name} [{Id.ToString()[..8]}]";

        #region 生命周期钩子（供派生类扩展）

        /// <summary>主动连接到下游节点后触发（此时拓扑已一致）。</summary>
        /// <param name="target">新连接的下游节点。</param>
        protected virtual void OnConnected(IXNode target) { }

        /// <summary>被上游节点连接后触发（此时拓扑已一致）。</summary>
        /// <param name="source">新连接的上游节点。</param>
        protected virtual void OnReceivedConnection(IXNode source) { }

        /// <summary>主动断开与下游节点的连接后触发（此时拓扑已一致）。</summary>
        /// <param name="target">被断开的下游节点。</param>
        protected virtual void OnDisconnected(IXNode target) { }

        /// <summary>失去上游节点的连接后触发（此时拓扑已一致）。</summary>
        /// <param name="source">被断开的上游节点。</param>
        protected virtual void OnLostConnection(IXNode source) { }

        #endregion
    }
}
