namespace ReluctersteSDK.DataStructures.Interfaces
{
    /// <summary>
    /// 带身份的图节点契约：在 <see cref="IXNode"/> 的拓扑能力之上，追加唯一标识与显示名称。
    /// </summary>
    /// <remarks>
    /// 只需要拓扑能力时请面向 <see cref="IXNode"/> 编程；需要标识/命名时用本接口。
    /// 唯一实现为 <c>NodeBase</c>。
    /// </remarks>
    public interface INode : IXNode
    {
        /// <summary>节点的唯一标识符。</summary>
        Guid Id { get; }

        /// <summary>节点的显示名称。</summary>
        string Name { get; set; }
    }
}
