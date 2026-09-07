namespace ReluctersteSDK.DataStructures.Interfaces
{
    /// <summary>
    /// 表示图结构中的顶点契约。
    /// </summary>
    /// <typeparam name="TVertex">实现该接口的顶点具体类型。</typeparam>
    /// <typeparam name="TEdge">关联的有向边类型，必须实现 <see cref="IGraphEdge{TVertex, TEdge}"/>。</typeparam>
    public interface IGraphVertex<TVertex, TEdge>
        where TVertex : IGraphVertex<TVertex, TEdge>
        where TEdge : IGraphEdge<TVertex, TEdge>
    {
        /// <summary>获取从当前顶点出发的所有出边集合。</summary>
        IReadOnlyCollection<TEdge> OutgoingEdges { get; }
        /// <summary>获取指向当前顶点的所有入边集合。</summary>
        IReadOnlyCollection<TEdge> IncomingEdges { get; }

        /// <summary>
        /// 向当前顶点的出边集合中添加一条边。
        /// </summary>
        /// <param name="edge">要添加的出边。</param>
        void AddOutgoingEdge(TEdge edge);
        /// <summary>
        /// 向当前顶点的入边集合中添加一条边。
        /// </summary>
        /// <param name="edge">要添加的入边。</param>
        void AddIncomingEdge(TEdge edge);
        /// <summary>
        /// 从当前顶点的出边集合中移除指定的边。
        /// </summary>
        /// <param name="edge">要移除的出边。</param>
        /// <returns>若成功移除则返回 <see langword="true"/>；若边不存在于集合中则返回 <see langword="false"/>。</returns>
        bool RemoveOutgoingEdge(TEdge edge);
        /// <summary>
        /// 从当前顶点的入边集合中移除指定的边。
        /// </summary>
        /// <param name="edge">要移除的入边。</param>
        /// <returns>若成功移除则返回 <see langword="true"/>；若边不存在于集合中则返回 <see langword="false"/>。</returns>
        bool RemoveIncomingEdge(TEdge edge);

        /// <summary>获取转换为具体派生类型的当前顶点实例。</summary>
        TVertex Self => (TVertex)this;
    }
}
