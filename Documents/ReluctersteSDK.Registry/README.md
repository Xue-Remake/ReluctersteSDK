# ReluctersteSDK.Registry

线程安全的注册表工具集。核心设计只有一句话：**条目不可变 + `ConcurrentDictionary` 原子替换**，因此读路径完全无锁。

- 目录：`ReluctersteSDK.Registry/`
- 包 ID：`ReluctersteSDK.Registry`
- 命名空间：`ReluctersteSDK.Registry`（JSON 相关在 `ReluctersteSDK.Registry.Json`）
- 依赖：无

五种注册表按"键是什么"来选：

| 想要什么 | 用哪个 |
|---|---|
| 一个键对一个值 | `ValueRegistry<TKey, TValue>` |
| 多列联合主键 | `CompositeValueRegistry<TValue>` |
| 抽象类型 → 具体类型（每次新建） | `TypeRegistry` |
| 类型 → 单例（或懒加载单例） | `InstanceRegistry` |
| 名字 → 委托（按字符串触发） | `DelegateRegistry` |

---

## 场景一：游戏设置项的热更新与启停

**目标**：把一组配置项放在表里，支持运行时开关某一项，且关闭后读取方拿不到它的值。

```csharp
using ReluctersteSDK.Registry;

var settings = new ValueRegistry<string, int>();

settings.Register("volume", 80);
settings.Register("fov", 90);
settings.Disable("fov");                 // 暂时关掉这一项

settings.GetValue("volume");             // 80
settings.GetValue("fov");                // 0 —— default(int)，因为被禁用
settings.Exists("fov");                  // true —— 物理上还在
settings.IsEnabled("fov");               // false

settings.Enable("fov");
settings.GetValue("fov");                // 90
```

要点：**禁用不等于删除**。值仍然保存在内部，只是所有读取接口会把它过滤掉；重新 `Enable` 就恢复。这个语义在五种注册表里是统一的。

---

## 场景二：按「年份 + 赛季 + 区域」查一份配置

**目标**：主键不是单列，而是多列组合。

```csharp
var index = new CompositeValueRegistry<string>();

// 键在前，值在最后
index.Register(2026, "season", "autumn", "秋季规则集");
index.Register("player", 1, "第一个玩家");

index.GetValue(2026, "season", "autumn");     // "秋季规则集"
index.TryGetValue("player", 1, out var name); // true, "第一个玩家"

// 只匹配前 N 列
var allPlayers = index.GetValuesByPrefix("player");
```

列数不限（`Register` 提供 1~4 列强类型重载，更多列用 `CompositeKey.From(...)`）。相等性按列依次比较。

> 复合键的列类型在 JSON 往返后会退化（`long` → `int`），见 [API/Json.md](API/Json.md)。

---

## 场景三：按接口取出仓储实现

**目标**：业务代码只认接口，具体实现从注册表里创建。

```csharp
public interface IRepository { string Name { get; } }

public class SqlRepository : IRepository
{
    public string Name => "sql";
}

var types = new TypeRegistry();

types.Register<IRepository, SqlRepository>();      // 反射构造，每次 CreateInstance 都新建
// 或：types.Register<IRepository>(() => new SqlRepository());

IRepository repo = types.CreateInstance<IRepository>();
```

想切换成测试替身，重新 `Register` 一次即可——重复注册会**覆盖**旧映射，并清掉另一种来源（具体类型 / 工厂），不会出现"工厂优先导致新映射被静默忽略"。

---

## 场景四：全局共享一个服务实例

**目标**：整个程序只用一个 `DataService`，但不想引入完整 DI 容器。

```csharp
var services = new InstanceRegistry();

services.RegisterInstance(new DataService());          // 现成实例
services.RegisterFactory(() => new CacheService());    // 懒加载：首次 Resolve 才创建

DataService data  = services.Resolve<DataService>();
CacheService cache = services.Resolve<CacheService>(); // 第二次拿到同一个
```

键是 `typeof(T)`，**注册时用的泛型参数决定了后面能用什么类型解析**。想按接口解析就写 `RegisterInstance<IDataService>(impl)`。

工厂用 `Lazy(..., PublicationOnly)`：并发解析时只采用一次成功结果，且**工厂抛出的异常不会被缓存**，下次可重试。

---

## 场景五：把行为挂到字符串名字上

**目标**：策划表里写的是命令名字符串，运行时按名字调用对应方法。

```csharp
var actions = new DelegateRegistry();

actions.Register("heal", () => player.Hp = player.MaxHp);
actions.Register("damage", (int amount) => player.Hp -= amount);
actions.Register("crit", (int baseDamage) => baseDamage * 2);

actions.Invoke("heal");
actions.Invoke("damage", 10);

int doubled = actions.Invoke<int, int>("crit", 21);   // 42
```

名字匹配是 `Ordinal`（区分大小写）。`Invoke` 优先尝试直接强转成 `Action` / `Func<...>` 调用，不匹配时回退到 `DynamicInvoke`（较慢，且异常会被包一层）。

---

## 场景六：把注册表存进 JSON

```csharp
using System.Text.Json;

var reg = new ValueRegistry<string, int>();
reg.Register("hp", 100);
reg.Register("mp", 50);
reg.Disable("mp");

string json = JsonSerializer.Serialize(reg);
```

```json
[
  { "key": "hp", "value": 100, "enabled": true },
  { "key": "mp", "value": 50, "enabled": false }
]
```

三个注册表类型都带 `[JsonConverter]`，直接序列化即可，禁用状态一并保留并在反序列化时还原。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/ValueRegistries.md](API/ValueRegistries.md) | `ValueRegistry<TKey,TValue>`、`CompositeKey`、`CompositeValueRegistry<TValue>` 的完整成员与语义 |
| [API/FactoryRegistries.md](API/FactoryRegistries.md) | `TypeRegistry`、`InstanceRegistry`、`DelegateRegistry` |
| [API/Json.md](API/Json.md) | 序列化格式、转换器、`RegistryJson` 选项助手 |
