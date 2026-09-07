using ReluctersteSDK.DataStructures.Interfaces;

namespace ReluctersteSDK.DataStructures.Structures
{
    /// <summary>
    /// 表示由顶点与有向边组成的泛型图数据结构。
    /// </summary>
    /// <typeparam name="TVertex">顶点的引用类型，必须实现 <see cref="IGraphVertex{TVertex, TEdge}"/>。</typeparam>
    /// <typeparam name="TEdge">有向边的引用类型，必须实现 <see cref="IGraphEdge{TVertex, TEdge}"/>。</typeparam>
    public class Graph<TVertex, TEdge>
    where TVertex : class, IGraphVertex<TVertex, TEdge>
    where TEdge : class, IGraphEdge<TVertex, TEdge>
    {
        private readonly HashSet<TVertex> _vertices = new();
        private readonly HashSet<TEdge> _edges = new();

        /// <summary>获取当前图中包含的所有顶点的只读集合。</summary>
        public IReadOnlyCollection<TVertex> Vertices => _vertices;
        /// <summary>获取当前图中包含的所有有向边的只读集合。</summary>
        public IReadOnlyCollection<TEdge> Edges => _edges;

        /// <summary>
        /// 向图中添加一个独立顶点。
        /// </summary>
        /// <param name="vertex">要添加的顶点。</param>
        /// <returns>若顶点添加成功返回 <see langword="true"/>；若顶点已存在于图中则返回 <see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="vertex"/> 为 <see langword="null"/> 时引发。</exception>
        public bool AddVertex(TVertex vertex)
        {
            ArgumentNullException.ThrowIfNull(vertex);
            return _vertices.Add(vertex);
        }

        /// <summary>
        /// 从图中移除指定顶点，并级联移除所有与其关联的入边和出边。
        /// </summary>
        /// <param name="vertex">要移除的顶点。</param>
        /// <returns>若顶点成功被移除返回 <see langword="true"/>；若图中不存在该顶点则返回 <see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="vertex"/> 为 <see langword="null"/> 时引发。</exception>
        public bool RemoveVertex(TVertex vertex)
        {
            ArgumentNullException.ThrowIfNull(vertex);
            if (!_vertices.Contains(vertex)) return false;

            // 移除关联的出边
            foreach (var edge in new List<TEdge>(vertex.OutgoingEdges))
            {
                RemoveEdge(edge);
            }

            // 移除关联的入边
            foreach (var edge in new List<TEdge>(vertex.IncomingEdges))
            {
                RemoveEdge(edge);
            }

            return _vertices.Remove(vertex);
        }
        /// <summary>
        /// 向图中添加一条有向边。若该边的源顶点或目标顶点未在图中注册，将自动将其加入图中。
        /// </summary>
        /// <param name="edge">要添加的有向边。</param>
        /// <returns>若边添加成功返回 <see langword="true"/>；若该边已存在于图中则返回 <see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="edge"/> 为 <see langword="null"/> 时引发。</exception>
        public bool AddEdge(TEdge edge)
        {
            ArgumentNullException.ThrowIfNull(edge);

            // 确保端点存在于图中
            AddVertex(edge.Source);
            AddVertex(edge.Target);

            if (_edges.Add(edge))
            {
                edge.Source.AddOutgoingEdge(edge);
                edge.Target.AddIncomingEdge(edge);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 从图中移除指定的有向边，并同步清理该边在源顶点与目标顶点中的关联状态。
        /// </summary>
        /// <param name="edge">要移除的有向边。</param>
        /// <returns>若边成功被移除返回 <see langword="true"/>；若图中不存在该边则返回 <see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="edge"/> 为 <see langword="null"/> 时引发。</exception>
        public bool RemoveEdge(TEdge edge)
        {
            ArgumentNullException.ThrowIfNull(edge);

            if (_edges.Remove(edge))
            {
                edge.Source.RemoveOutgoingEdge(edge);
                edge.Target.RemoveIncomingEdge(edge);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 确定指定顶点是否存在于图中。
        /// </summary>
        /// <param name="vertex">要在图中定位的顶点。</param>
        /// <returns>若顶点存在于图中则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool ContainsVertex(TVertex vertex) => _vertices.Contains(vertex);
        /// <summary>
        /// 确定指定有向边是否存在于图中。
        /// </summary>
        /// <param name="edge">要在图中定位的边。</param>
        /// <returns>若边存在于图中则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool ContainsEdge(TEdge edge) => _edges.Contains(edge);
        /// <summary>
        /// 清空图中的所有顶点与边。
        /// </summary>
        /// <remarks>
        /// 注意：此操作仅清空图容器内部的顶点与边索引，不会遍历清除各个顶点实例内部维护的边关联列表。
        /// </remarks>
        public void Clear()
        {
            _vertices.Clear();
            _edges.Clear();
        }
    }
}
