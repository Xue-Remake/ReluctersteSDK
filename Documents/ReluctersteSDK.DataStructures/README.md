# ReluctersteSDK.DataStructures

标准库集合没覆盖的四种结构：**双向字典、保序线程安全字典、有向图、拓扑节点**。

- 目录：`ReluctersteSDK.DataStructures/`
- 包 ID：`ReluctersteSDK.DataStructures`
- 命名空间：`ReluctersteSDK.DataStructures.Structures` / `.Interfaces` / `.Json`
- 依赖：无

---

## 场景一：用 ID 查名字，也用名字查 ID

**目标**：一张表两边都要能反查，且两边都必须唯一。

```csharp
using ReluctersteSDK.DataStructures.Structures;

var map = new BiDictionary<int, string>();
map.Add(1001, "铁剑");
map.Add(1002, "木盾");

map.GetValue(1001);          // "铁剑"   正向
map.GetKey("木盾");          // 1002     反向

map.ContainsValue("铁剑");   // true
map.RemoveByKey(1001);       // 反向映射一起删掉
```

`Add` 在**键或值**重复时都会抛 `ArgumentException`。需要"有则覆盖"时用 `AddOrUpdate`：

```csharp
map.AddOrUpdate(1002, "钢盾");   // 先解除两侧的旧映射，再写入新的一对
```

---

## 场景二：维护一张有顺序的配置表

**目标**：配置项要按声明顺序显示，同时能被多个线程读写。

```csharp
using var table = new ListDictionary<string, int>();

table.Add("width", 1920);        // 追加到末尾
table.Add("height", 1080);
table.SetValue("width", 2560);   // 有则改，无则追加；顺序不变

table[0];                        // KeyValuePair("width", 2560) —— 按下标访问
table.Count;                     // 2

table.Sort(kv => kv.Value, descending: true);
```

读写全部走 `ReaderWriterLockSlim`，枚举返回**调用时刻的快照**：

```csharp
foreach (var kv in table) { /* 遍历期间其他线程的修改不影响本次循环 */ }
```

内部是 `List` + 线性查找，**查找 O(N)**。适合元素少、需要保持插入顺序的场景；元素多请用 `Dictionary`。

> `ListDictionary` 实现了 `IDisposable`（释放内部锁），记得 `using`。

---

## 场景三：表达一张有向依赖图

**目标**：顶点和边都是自己的类型，需要知道谁指向谁。

```csharp
using ReluctersteSDK.DataStructures.Interfaces;
using ReluctersteSDK.DataStructures.Structures;

public class TaskVertex : IGraphVertex<TaskVertex, TaskEdge>
{
    public string Name { get; init; } = "";

    private readonly List<TaskEdge> _out = new();
    private readonly List<TaskEdge> _in = new();

    public IReadOnlyCollection<TaskEdge> OutgoingEdges => _out;
    public IReadOnlyCollection<TaskEdge> IncomingEdges => _in;

    public void AddOutgoingEdge(TaskEdge e) => _out.Add(e);
    public void AddIncomingEdge(TaskEdge e) => _in.Add(e);
    public bool RemoveOutgoingEdge(TaskEdge e) => _out.Remove(e);
    public bool RemoveIncomingEdge(TaskEdge e) => _in.Remove(e);
}

public class TaskEdge : IGraphEdge<TaskVertex, TaskEdge>
{
    public TaskVertex Source { get; init; } = null!;
    public TaskVertex Target { get; init; } = null!;
}
```

```csharp
var graph = new Graph<TaskVertex, TaskEdge>();
var build = new TaskVertex { Name = "build" };
var test  = new TaskVertex { Name = "test" };

graph.AddEdge(new TaskEdge { Source = build, Target = test });  // 端点未注册会自动加入
graph.RemoveVertex(build);                                       // 级联删掉关联的出入边
```

顶点/边用 `HashSet` 去重，**依赖引用相等**（默认 `Equals`）。要值语义请自己重写。

---

## 场景四：搭一条会自我维护的节点链

**目标**：节点之间连线时，双方都要知道对方；连线/断线时想触发副作用。

```csharp
using ReluctersteSDK.DataStructures.Interfaces;
using ReluctersteSDK.DataStructures.Structures;

public class FlowNode : NodeBase
{
    public FlowNode(string? name = null) : base(name) { }

    protected override void OnConnected(IXNode target)
        => Console.WriteLine($"→ {(target as INode)?.Name}");

    protected override void OnReceivedConnection(IXNode source)
        => Console.WriteLine($"← {(source as INode)?.Name}");
}

var a = new FlowNode("输入");
var b = new FlowNode("处理");

a.ConnectTo(b);          // 打印：→ 处理 / ← 输入
a.DisconnectFrom(b);
a.ClearConnections();    // 切断自己所有出入连接，对端同步清理
```

`ConnectTo` 会同时写 `a.Outputs` 和 `b.Inputs`——**不要手工改这两个列表**，它们是只读视图。

> 默认**禁自环、禁重复连线，但不禁环**。要禁环就重写 `CanConnectTo`：
> ```csharp
> public override bool CanConnectTo(IXNode target)
>     => base.CanConnectTo(target) && !WouldCreateCycle(target);
> ```

---

## 场景五：把这两种集合序列化

```csharp
using System.Text.Json;

var map = new BiDictionary<string, int> { { "a", 1 } };
string json = JsonSerializer.Serialize(map);
// [ { "key": "a", "value": 1 } ]
```

`BiDictionary` 与 `ListDictionary` 都带 `[JsonConverter]`，直接序列化即可。`Graph` 与 `NodeBase` 没有转换器（它们持有引用关系，不适合直接落盘）。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/Collections.md](API/Collections.md) | `BiDictionary<TKey,TValue>`、`ListDictionary<TKey,TValue>` 的完整成员与并发语义 |
| [API/Graph.md](API/Graph.md) | `Graph<TVertex,TEdge>` 与 CRTP 约束 |
| [API/Node.md](API/Node.md) | `IXNode` / `INode` / `NodeBase`：连接规则、钩子、成环检测 |
| [API/Json.md](API/Json.md) | 序列化格式与 `DataStructuresJson` 选项助手 |
