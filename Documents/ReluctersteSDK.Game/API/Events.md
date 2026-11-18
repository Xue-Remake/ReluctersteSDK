# API：双缓冲事件

命名空间：`ReluctersteSDK.Game.Ecs.Events`

- [`WorldEvents`](#worldevents)
- [`EventQueue<T>`](#eventqueuet)
- [交换时机与语义](#交换时机与语义)
- [使用模式](#使用模式)

---

## `WorldEvents`

`World.Events` 的类型。按事件类型组织的双缓冲容器。

| 成员 | 说明 |
|---|---|
| `Queue<T>()` | 取该类型的队列；不存在时创建。热路径可缓存返回值 |
| `Raise<T>(in T e)` | 登记事件（内部查一次类型表） |
| `Read<T>()` | 读取上一批事件，返回 `ReadOnlySpan<T>`；未登记过时返回空 span 且**不创建队列** |
| `ReadList<T>()` | 同上但返回 `IReadOnlyList<T>`；未登记过时返回 `Array.Empty<T>()` |
| `PendingCountOf<T>()` | 该类型尚未交换的条数 |
| `QueueCount` | 已创建的队列数量 |
| `PendingCount` | 所有队列待交换的总数 |
| `Swap()` | 交换所有队列的读写缓冲；由 `World.Update` 自动调用 |
| `Clear()` | 清空读写两侧 |

---

## `EventQueue<T>`

```csharp
public sealed class EventQueue<T> : IEventQueue where T : notnull
{
    public int PendingCount { get; }
    public ReadOnlySpan<T> Read { get; }
    public IReadOnlyList<T> ReadList { get; }
    public void Raise(in T e);
    public void Swap();
    public void Clear();
}
```

### `Read` 与 `ReadList` 的取舍

两者都**零分配**，区别只在生命周期与遍历方式：

| | `Read`（span） | `ReadList`（`IReadOnlyList<T>`） |
|---|---|---|
| 分配 | 0 | 0 |
| `ref readonly` 遍历 | ✅ | ❌ |
| 可存进局部变量 / 传给方法 | ⚠️ 只能短时使用 | ✅ 可以传 |
| 下一次 `Swap()` 后 | 失效（野指针风险） | 旧引用被**清空**，不崩但内容消失 |
| 适用 | 热路径、要改组件 | 普通遍历、要传给别的方法 |

`ReadList` 不是"安全版"，只是"不会变成野指针版"：

```csharp
// 读缓冲由两个列表交替复用：
//   Swap #1 → 引用 A（[item1]）
//   Swap #2 → A 被清空，新的 ReadList 是 B（[item2]）
IReadOnlyList<ItemPicked> held = queue.ReadList;   // A
queue.Swap();
Assert.Empty(held);                                // A 已被清空
```

**需要跨帧保存必须自己复制**（`ToArray()` / `ToList()`）。

### 为什么遍历时可以继续 `Raise`

写缓冲与读缓冲是两个独立的 `List<T>` 对象。遍历读缓冲期间，`Raise` 写入的是另一个对象，因此不存在迭代器失效或集合被修改异常。

这是双缓冲相比"单列表 + 快照"最直接的价值。

---

## 交换时机与语义

`World.Update` 的执行顺序：

```
EnsureNotDisposed()
StartIfNeeded()          ← 首次调用会跑 Startup 阶段
Events.Swap()            ← 交换事件缓冲
ExecutePhase(Update)
ExecutePhase(LateUpdate)
Commands.Playback()
```

由此得到两条规则：

| 场景 | 结果 |
|---|---|
| 在 **Update / LateUpdate / Render 期间** `Raise` | 下一帧的 `Update` 之后可读（延迟 1 帧） |
| 在 **Startup 阶段** `Raise` | 首帧即可读（因为在 `Swap` 之前） |
| 在 `Update` 之外（如 UI 回调）随时 `Raise` | 下一次 `Update` 开始时可读 |

**所有阶段看到的是同一份读缓冲**，不存在"Update 看不到、Render 看得到"的分裂。

### 为什么放在开头而不是结尾

放在结尾会导致：`Update` 系统读上一帧、`Render` 系统读本帧，语义分裂，排查成本很高。
放在开头则统一为"上一次交换到这一次交换之间的所有事件"。

---

## 使用模式

### 1. 定义事件

用 `readonly record struct`，零分配：

```csharp
using ReluctersteSDK.Game.Ecs;

public readonly record struct DamageDealt(Entity Attacker, Entity Target, int Amount);
public readonly record struct EntityDied(Entity Victim, Entity Killer);
```

不需要继承任何基类，也没有注册步骤。

### 2. 产生

```csharp
public sealed class CombatSystem : ISystem
{
    public void Execute(World world, float deltaTime)
    {
        world.Query<Health, AttackTarget>().ForEach((Entity e, ref Health h, ref AttackTarget t) =>
        {
            if (!world.IsAlive(t.Target))
                return;

            ref Health target = ref world.Get<Health>(t.Target);
            int dealt = SystemMath.Min(h.Attack, target.Value);
            target.Value -= dealt;

            world.Events.Raise(new DamageDealt(e, t.Target, dealt));
        });
    }
}
```

### 3. 消费

```csharp
public sealed class DeathSystem : ISystem
{
    public void Execute(World world, float deltaTime)
    {
        foreach (ref readonly DamageDealt e in world.Events.Read<DamageDealt>())
        {
            if (!world.IsAlive(e.Target))
                continue;

            ref Health health = ref world.Get<Health>(e.Target);
            if (health.Value <= 0)
                world.Commands.Destroy(e.Target);   // 结构修改走命令缓冲
        }
    }
}
```

> 在事件消费里**直接** `world.Add/Destroy` 是安全的（不在 `Query.ForEach` 内）；
> 走 `Commands` 只是为了让修改时机更可控。

### 4. 热路径缓存队列

每帧上千次 `Raise` 时，可以避开类型查找：

```csharp
private EventQueue<DamageDealt> _damage = null!;

public void OnStart(World world) => _damage = world.Events.Queue<DamageDealt>();

public void Execute(World world, float deltaTime)
{
    _damage.Raise(new DamageDealt(...));
}
```

### 5. 桥接到外部

`Game` 不依赖 `EventSystem`。需要广播给 UI / 存档 / 遥测时，在应用层桥接：

```csharp
public sealed class EventBridgeSystem : ISystem
{
    private readonly IEventBus _bus;

    public void Execute(World world, float deltaTime)
    {
        foreach (ref readonly DamageDealt e in world.Events.Read<DamageDealt>())
            _ = _bus.PublishAsync(new DamageDealtEvent(e.Attacker, e.Target, e.Amount));
    }
}
```

---

## 明确不做

| 不做 | 理由 |
|---|---|
| 异步 / `Task` | 跨帧或跨线程就该由外部总线负责 |
| 委托订阅（`Subscribe`） | 引入处理器就要定义执行顺序、重入与异常传播，等于再造一个总线 |
| 类型继承分发 | 类型即队列；`Raise<Derived>` 只有 `Read<Derived>` 看得到 |
| 事件序列化 | 事件是"这一帧发生过什么"，不是状态，不进快照 |

## 已知限制

- **非线程安全**：`Raise` / `Read` / `Swap` 都必须在主线程调用。跨线程请用 `ConcurrentQueue` 中转。
- **`Read` 的 span 会被下一次 `Swap` 作废**。
- **单帧延迟**：本帧登记的事件本帧读不到（Startup 除外）。
- **`Clear` 会同时丢弃读缓冲**：如果在一帧中间调用 `World.Clear()`，上一批未消费的事件会一并消失。
