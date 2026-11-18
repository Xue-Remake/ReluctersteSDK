# API：拓扑节点

命名空间：`ReluctersteSDK.DataStructures.Interfaces` / `.Structures`

两层接口 + 一个实现：

| 类型 | 内容 | 什么时候用 |
|---|---|---|
| `IXNode` | 只有拓扑：`Inputs` / `Outputs` / `ConnectTo` / `DisconnectFrom` / `ClearConnections` / `CanConnectTo` | 只需要"能连"时面向它编程 |
| `INode : IXNode` | 追加 `Id`（`Guid`）与 `Name` | 需要标识 / 命名 |
| `NodeBase : INode` | 唯一实现：双向连接维护 + 四条生命周期钩子 | 继承它写业务节点 |

> 历史上曾有一个平行的 `XNodeBase`（无 Id/Name 的纯拓扑基类），因与 `NodeBase` 逐行重复已删除。
> 现在需要"轻量"就面向 `IXNode` 接口，而不是另立一个基类。

---

## `IXNode`

| 成员 | 返回 | 说明 |
|---|---|---|
| `Inputs` | `IReadOnlyList<IXNode>` | 上游节点 |
| `Outputs` | `IReadOnlyList<IXNode>` | 下游节点 |
| `ConnectTo(IXNode target)` | `bool` | 建立 `this → target`，双向登记 |
| `DisconnectFrom(IXNode target)` | `bool` | 断开 `this → target` |
| `ClearConnections()` | `void` | 切断自己所有出入连接 |
| `CanConnectTo(IXNode target)` | `bool` | 连接前校验 |

## `INode`

```csharp
public interface INode : IXNode
{
    Guid Id { get; }
    string Name { get; set; }
}
```

## `NodeBase`

```csharp
protected NodeBase(string? name = null, Guid? id = null)
```

| 成员 | 说明 |
|---|---|
| `Id` | 构造时生成（或传入），**之后只读** |
| `Name` | 未指定时用运行时类型名；**设为 null 抛 `ArgumentNullException`** |
| `Inputs` / `Outputs` | `ReadOnlyCollection` 视图，无法强转回 `List` 后篡改 |
| `InputCount` / `OutputCount` | 计数 |
| `CanConnectTo(IXNode)` | `virtual`，默认规则见下 |
| `ConnectTo(IXNode)` | 非虚，走 `CanConnectTo` |
| `DisconnectFrom(IXNode)` | 非虚 |
| `ClearConnections()` | `virtual` |
| `WouldCreateCycle(IXNode)` | 成环检测，不自动启用 |
| `ToString()` | `"{Name} [{Id 前 8 位}]"` |

### 默认连接规则

`CanConnectTo(target)` 返回 `true` 当且仅当：

1. `target` 不为 `null`；
2. `target` 不是自己（禁自环）；
3. `target` 是 `NodeBase`（**接口参数，实现必须是本基类**）；
4. 尚未连过（禁重复连接）。

**不禁止成环**——有向图的语义由使用方决定。

### 为什么只能是 `NodeBase`

连接是双向登记：`a.ConnectTo(b)` 要往 `b._inputs` 里写。`IXNode` 接口只暴露只读列表，无法写入别人的内部状态，所以实现层强制要求对方也是 `NodeBase`。

想让其它实现可连接，只能自己实现一整套双向登记逻辑——不是本库支持的方向。

### 钩子

| 钩子 | 触发时机 |
|---|---|
| `OnConnected(IXNode target)` | `this` 主动连上 `target` 之后 |
| `OnReceivedConnection(IXNode source)` | `this` 被 `source` 连上之后 |
| `OnDisconnected(IXNode target)` | `this` 主动断开 `target` 之后 |
| `OnLostConnection(IXNode source)` | `this` 被 `source` 断开之后 |

四条钩子都在**两侧拓扑已经改完之后**调用，钩子里读连接关系是安全的。参数类型是 `IXNode`，需要名字时转成 `INode`。

```csharp
protected override void OnDisconnected(IXNode target)
    => Console.WriteLine($"断开 {(target as INode)?.Name}");
```

### `ClearConnections` 的顺序

1. 倒序遍历 `_outputs`，逐条 `DisconnectFrom`；
2. 倒序遍历 `_inputs`，对每个 `NodeBase` 上游调 `source.DisconnectFrom(this)`。

因此上游的 `OnDisconnected` 与自己的 `OnLostConnection` 都会正常触发。

### `WouldCreateCycle`

```csharp
public bool WouldCreateCycle(IXNode target)
```

从 `target` 出发沿 **`Outputs`** 方向做 DFS，能走到 `this` 就说明 `this → target` 会成环。

方向不要搞反：成环 ⟺ `target` 已经能顺流到达 `this`。

启用禁环：

```csharp
public override bool CanConnectTo(IXNode target)
    => base.CanConnectTo(target) && !WouldCreateCycle(target);
```

复杂度 O(V+E)，每次连接调用一次，链很长时会有开销。

---

## 已知限制

- **只有 `NodeBase` 之间能连**：其它 `IXNode` 实现一律被拒。
- **默认不禁环**：需要自己重写 `CanConnectTo`。
- **单条连接没有"权重/端口/标签"**：`Inputs` / `Outputs` 只是节点列表，多元语义要自己记。
- **`ClearConnections` 不是原子的**：中途抛异常会留下部分连线。
- **没有序列化支持**：`NodeBase` 不带 `[JsonConverter]`，且连接图是引用结构，落盘需要自己设计 id 映射。
- **`Id` 不可变**：需要重建身份就换对象。
- **`Name` 不参与相等性**：`NodeBase` 没有重写 `Equals`，两个同名节点是两个不同的对象。
