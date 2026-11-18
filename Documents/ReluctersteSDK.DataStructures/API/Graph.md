# API：有向图

命名空间：`ReluctersteSDK.DataStructures.Structures` / `.Interfaces`

`Graph<TVertex, TEdge>` 是一个**容器**：它持有顶点与边的集合，并负责在增删边时同步更新顶点自身的出入边列表。

---

## CRTP 约束

```csharp
public class Graph<TVertex, TEdge>
    where TVertex : class, IGraphVertex<TVertex, TEdge>
    where TEdge   : class, IGraphEdge<TVertex, TEdge>
```

`TVertex` 与 `TEdge` 都必须实现一个以**自身与对方**为泛型参数的接口（奇异递归模板）。这样 `Graph` 才能在不知道具体类型的情况下调用 `edge.Source.AddOutgoingEdge(edge)` 并拿到正确类型。

---

## `IGraphVertex<TVertex, TEdge>`

| 成员 | 说明 |
|---|---|
| `OutgoingEdges` | `IReadOnlyCollection<TEdge>`，出边 |
| `IncomingEdges` | `IReadOnlyCollection<TEdge>`，入边 |
| `AddOutgoingEdge(TEdge)` | 由 `Graph.AddEdge` 调用，实现方负责存进自己的集合 |
| `AddIncomingEdge(TEdge)` | 同上 |
| `RemoveOutgoingEdge(TEdge)` → `bool` | 返回是否真的移除 |
| `RemoveIncomingEdge(TEdge)` → `bool` | 同上 |
| `Self` | 默认接口实现 `(TVertex)this`，避免实现方手写强转 |

**这 4 个 `Add*` / `Remove*` 方法由 `Graph` 调用，不要自己直接调**——绕过 `Graph` 会让容器与顶点的视图不一致。

## `IGraphEdge<TVertex, TEdge>`

| 成员 | 说明 |
|---|---|
| `Source` | 起点顶点 |
| `Target` | 终点顶点 |

---

## `Graph<TVertex, TEdge>`

| 成员 | 返回 | 说明 |
|---|---|---|
| `Vertices` | `IReadOnlyCollection<TVertex>` | 顶点集合（内部 `HashSet`） |
| `Edges` | `IReadOnlyCollection<TEdge>` | 边集合（内部 `HashSet`） |
| `AddVertex(TVertex)` | `bool` | 已存在返回 `false`；null 抛 `ArgumentNullException` |
| `RemoveVertex(TVertex)` | `bool` | **级联移除**所有关联出入边 |
| `AddEdge(TEdge)` | `bool` | 端点未注册时**自动加入**；边已存在返回 `false` |
| `RemoveEdge(TEdge)` | `bool` | 同步清理两端顶点自己的边列表 |
| `ContainsVertex(TVertex)` | `bool` | |
| `ContainsEdge(TEdge)` | `bool` | |
| `Clear()` | `void` | 只清空容器索引，**不遍历清理顶点自己的边列表** |

### 添加/删除的副作用

`AddEdge(edge)`：

1. `AddVertex(edge.Source)`、`AddVertex(edge.Target)`（已存在则无操作）；
2. 若 `_edges.Add(edge)` 成功 → `edge.Source.AddOutgoingEdge(edge)`、`edge.Target.AddIncomingEdge(edge)`。

`RemoveVertex(v)`：

1. 复制一份 `v.OutgoingEdges`，逐条 `RemoveEdge`；
2. 复制一份 `v.IncomingEdges`，逐条 `RemoveEdge`；
3. 从顶点集合移除。

删除期间会**改写 `v` 自己的边列表**，所以内部先做了一份快照再遍历。

### 引用相等

`HashSet` 用默认 `Equals`。`Graph` 不做值语义包装：

```csharp
var a1 = new MyVertex { Name = "A" };
var a2 = new MyVertex { Name = "A" };
graph.AddVertex(a1);
graph.AddVertex(a2);   // true —— 两个不同实例
graph.Vertices.Count;  // 2
```

要用值语义请在自己的顶点/边类型里重写 `Equals` / `GetHashCode`。

### `Clear` 的陷阱

```csharp
graph.AddEdge(new MyEdge { Source = a, Target = b });
graph.Clear();

graph.Vertices.Count;      // 0
a.OutgoingEdges.Count;     // 仍然是 1 —— 顶点自己不知道图被清空了
```

`Clear()` 只清 `_vertices` 与 `_edges`。如果之后还要复用这些顶点，必须自己调用 `RemoveOutgoingEdge` / `RemoveIncomingEdge`，或改用 `RemoveVertex` 逐个删。

---

## 已知限制

- **无遍历算法**：不提供拓扑排序、最短路径、环检测；只有容器语义。
- **无权重**：权重需要自己挂在边上。
- **不支持多重边**：`HashSet` 去重意味着两个"相同"的边只能存一条（默认引用相等下，不同实例可以共存）。
- **`Vertices` / `Edges` 直接暴露内部集合的只读视图**，枚举期间被修改会抛 `InvalidOperationException`。
- **不是线程安全的**：所有操作都没有加锁。
- **`Clear` 不递归清理顶点状态**（见上）。
