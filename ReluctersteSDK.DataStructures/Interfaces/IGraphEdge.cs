namespace ReluctersteSDK.DataStructures.Interfaces
{
    /// <summary>
    /// 表示图结构中有向边的契约。
    /// </summary>
    /// <typeparam name="TVertex">边关联的顶点类型，必须实现 <see cref="IGraphVertex{TVertex, TEdge}"/>。</typeparam>
    /// <typeparam name="TEdge">实现该接口的边具体类型（CRTP 奇异递归模板模式）。</typeparam>
    public interface IGraphEdge<TVertex, TEdge>
        where TVertex : IGraphVertex<TVertex, TEdge>
        where TEdge : IGraphEdge<TVertex, TEdge>
    {
        /// <summary>获取该有向边的源顶点（起始点）。</summary>
        TVertex Source { get; }
        /// <summary>获取该有向边的目标顶点（终点）。</summary>
        TVertex Target { get; }
        /// <summary>获取转换为具体派生类型的当前边实例。</summary>
        TEdge Self => (TEdge)this;
    }
}
