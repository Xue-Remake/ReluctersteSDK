namespace ReluctersteSDK.DataStructures.Interfaces
{
    /// <summary>
    /// 基础图拓扑节点接口，仅提供核心的连接与断开能力
    /// </summary>
    public interface IXNode
    {
        /// <summary>当前节点的入边/输入节点集合（只读）</summary>
        IReadOnlyList<IXNode> Inputs { get; }

        /// <summary>当前节点指向的出边/输出节点集合（只读）</summary>
        IReadOnlyList<IXNode> Outputs { get; }

        /// <summary>
        /// 将当前节点连接到指定的目标节点（当前节点作为输入源: this -> target）
        /// </summary>
        /// <param name="target">目标下游节点</param>
        /// <returns>若连接成功建立则返回 true；若已被阻止或目标不合法则返回 false</returns>
        bool ConnectTo(IXNode target);

        /// <summary>
        /// 断开当前节点与指定目标节点的下游连接（this -x-> target）
        /// </summary>
        /// <param name="target">已连接的目标下游节点</param>
        /// <returns>若成功断开则返回 true；若连接不存在或断开失败则返回 false</returns>
        bool DisconnectFrom(IXNode target);

        /// <summary>
        /// 彻底切断当前节点所有的双向输入与输出连接
        /// </summary>
        void ClearConnections();

        /// <summary>
        /// 校验当前节点是否允许与指定目标节点建立连接
        /// </summary>
        /// <param name="target">待校验的目标下游节点</param>
        /// <returns>若允许建立连接则返回 true；否则返回 false</returns>
        bool CanConnectTo(IXNode target);
    }
}
