# ReluctersteSDK.Config

配置数据的**存储抽象层**：用 `IConfigStore<T>` 统一"存什么"，内存 / JSON 文件 / SQLite 三种后端可以互换。

- 目录：`ReluctersteSDK.Config/`
- 包 ID：`ReluctersteSDK.Config`
- 命名空间：`ReluctersteSDK.Config.Interfaces` / `.Stores` / `.Codecs` / `.Hosting`
- 依赖：`ReluctersteSDK.Paths`、`ReluctersteSDK.Sqlite`、`Microsoft.Extensions.DependencyInjection.Abstractions`

```
                 ┌──────────────────────────────┐
IConfig  ────────│  IConfigStore<T>             │
IEntity  ────────│    ├─ InMemoryConfigStore<T>  │   测试 / 临时
                 │    ├─ JsonFileConfigStore<T>  │   小文件
                 │    └─ SqliteConfigStore<T>    │   关系型
                 └──────────────────────────────┘
```

---

## 场景一：先跑起来（内存存储）

**目标**：单元测试或原型阶段，不想碰磁盘。

```csharp
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;

public class GameSetting : IConfig
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

var store = new InMemoryConfigStore<GameSetting>(s => s.Key);

ConfigSaveResult result = await store.SaveAsync(new GameSetting { Key = "volume", Value = "80" });
GameSetting? loaded = await store.GetAsync("volume");
```

`IConfig` 仍是**纯标记接口**，仅表示"这类型是配置"。

---

## 场景二：存成一个 JSON 文件

**目标**：不想要数据库，就一个小文件。

```csharp
using ReluctersteSDK.Paths;
using ReluctersteSDK.Config.Stores;

var store = new JsonFileConfigStore<GameSetting>(
    new RelativePath(@"Data\settings.json"),
    s => s.Key);

await store.SaveAsync(new GameSetting { Key = "volume", Value = "80" });
```

文件内容是配置数组，写入采用**临时文件 + 覆盖移动**，进程中途退出不会留下截断的 JSON。

默认 `WriteIndented = true`；需要改命名策略或换 `JsonSerializerContext` 时传入自己的选项：

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = GameConfigJsonContext.Default
};
var store = new JsonFileConfigStore<GameSetting>(path, s => s.Key, options);
```

---

## 场景三：存进 SQLite

**目标**：配置量大、需要按条件查询。

```csharp
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Sqlite;

[DbTable("settings")]
public class GameSetting : IConfig
{
    [DbKey("key")]    public string Key { get; set; } = "";
    [DbColumn("val")] public string Value { get; set; } = "";
}

await using var db = new SqliteDataBase(@"Data", "game.db");
var store = new SqliteConfigStore<GameSetting>(db);

await store.SaveAsync(new GameSetting { Key = "volume", Value = "80" });          // 覆盖
await store.SaveAsync(new GameSetting { Key = "lang", Value = "zh" }, SaveMode.Insert);

GameSetting? one = await store.GetAsync("volume");
IReadOnlyList<GameSetting> hit = await store.QueryAsync(
    new ConfigQuery<GameSetting> { Predicate = s => s.Key.StartsWith("ui.") });
```

**建表不用自己管**：首次访问会自动调用 `EnsureTableAsync<T>()`。

---

## 场景四：业务实体 ↔ 数据行

**目标**：内存里用业务模型，落库用数据行，两者需要互转。

```csharp
var inner = new SqliteConfigStore<ItemDataRow>(db);
var items = new EntityConfigStore<ItemEntity, ItemDataRow>(inner);

await items.SaveAsync(new ItemEntity { ItemId = 1001, Name = "Sword" });
ItemEntity? one = await items.GetAsync(new object?[] { 1001 });
IReadOnlyList<ItemEntity> found = await items.QueryAsync(
    new ConfigQuery<ItemDataRow> { Predicate = d => d.Rarity >= 3 });
```

物品与库存共用这一个适配器：差别只在实体实现 `IItemEntity` 还是 `IInventoryEntity`。
转换走 `ToData()` / `ToEntity()`，**不涉及反射**。

---

## 场景五：批量保存与错误处理

**目标**：一次写多条，失败时能知道写到第几条。

```csharp
ConfigSaveResult result = await store.SaveAsync(items, SaveMode.Insert);

if (!result.Success)
{
    Console.WriteLine($"写到第 {result.Written} 条失败：{result.Error?.Message}");
}
```

三条硬约束：

| 行为 | 说明 |
|---|---|
| 异常不再被吞掉 | 失败原因放在 `Error` 里，调用方可以记录/重试 |
| `Written` 表示实际写入条数 | 批量中途失败能判断部分写入 |
| `Insert` 先整体校验 | 内存与 JSON 后端遇到重复键时**整批不写** |

SQLite 后端例外：ORM 目前没有公开事务 API，批量是逐条提交的，失败时前序记录已落库 —— 用 `Written` 判断。

---

## 场景六：用源生成控制 JSON 格式

**目标**：不依赖运行时反射，同时固定字段名与格式（也为裁剪/AOT 做准备）。

```csharp
using System.Text.Json.Serialization;
using ReluctersteSDK.Config.Codecs;

[JsonSerializable(typeof(GameSetting))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GameConfigJson : JsonSerializerContext
{
}

var codec = JsonConfigCodec<GameSetting>.FromContext(GameConfigJson.Default, s => s.Key);
var store = new JsonFileConfigStore<GameSetting>(path, codec);
```

| 差异 | 走 `JsonSerializerOptions` | 走 `IConfigCodec<T>` |
|---|---|---|
| 元数据来源 | 运行时反射 | 编译期生成 |
| 类型白名单 | 无 | 上下文里声明了才有 |
| 主键 | 构造参数 `keySelector` | `codec.GetKey` |
| 适用 | 快速上手 | 裁剪/AOT、格式严格受控 |

`FromContext` 找不到类型元数据时会直接抛 `InvalidOperationException`，不会悄悄退回反射。

---

## 场景七：多模块配置节 + 变更通知

**目标**：每个模块自己注册配置，上层不需要集中知道有哪些配置。

```csharp
using ReluctersteSDK.Config.Hosting;

var registry = new ConfigRegistry();

// 各模块各自注册
registry.Register(new SqliteConfigStore<GameSetting>(db), "game");
registry.Register(new JsonFileConfigStore<AudioSetting>(audioPath, s => s.Key), "audio");

// 任意位置取用
var audio = registry.GetStore<AudioSetting>();

registry.Changed += (_, e) =>
    Console.WriteLine($"配置节 {e.Section} 写入了 {e.Written} 条");
```

- 节名默认是类型名（`AudioSetting`），注册时可覆盖。
- **只有保存成功且 `Written > 0` 才触发** `Changed`。
- 事件由注册表返回的包装存储触发；**绕过注册表直接用原始 store 不会通知**。

依赖注入：

```csharp
services.AddReluctersteSDKConfig(registry =>
{
    registry.Register(new SqliteConfigStore<GameSetting>(db), "game");
});

// 之后直接注入 ConfigRegistry
public MyService(ConfigRegistry config) { }
```

这里只依赖 `Microsoft.Extensions.DependencyInjection.Abstractions`，不会把容器实现拖进库。

---

## 取消与线程

所有异步方法都接受 `CancellationToken`，库内 `await` 统一 `ConfigureAwait(false)`。

- `InMemoryConfigStore` / `JsonFileConfigStore`：实例内用锁串行化读写，可多线程调用。
- `SqliteConfigStore`：并发语义由 `SqliteDataBase` 自己保证（写串行、读不加锁）。

---

## 从旧版迁移

0.13.0.0 之后，旧的静态 Manager 已删除，对照如下：

| 旧 API | 新 API |
|---|---|
| `ConfigManager.TrySaveAsync(db, x)` | `new SqliteConfigStore<T>(db).SaveAsync(x)` |
| `ConfigManager.TrySaveOrUpdateAsync(db, x)` | `SaveAsync(x, SaveMode.Upsert)` |
| `ConfigManager.GetByIdAsync<T>(db, id)` | `store.GetAsync(id)` |
| `ConfigManager.QueryAsync<T>(db, where, ...)` | `store.QueryAsync(new ConfigQuery<T> { Predicate = ... })` |
| `ConfigManager.TrySaveJsonAsync(path, x)` | `new JsonFileConfigStore<T>(path, key).SaveAsync(x)` |
| `JsonConfigManager.*` | 已删除；自定义格式直接注入 `JsonSerializerOptions` |
| `ItemManager.*` / `InventoryManager.*` | `EntityConfigStore<TEntity, TData>` |
| `IJsonConfig` | 已删除 |

查询条件从 SQL 字符串改成了**谓词**，这样三种后端语义一致；代价是 SQLite 后端会把整表读出来再过滤（配置数据量小，可以接受）。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/Interfaces.md](API/Interfaces.md) | `IConfig`、`IData<TEntity>`、`IEntity<TSelf,TData>`、`IItemEntity`、`IInventoryEntity` |
| [API/Stores.md](API/Stores.md) | `IConfigStore<T>`、`ConfigQuery<T>`、`ConfigSaveResult`、三种实现、实体适配器 |
| [API/Codecs.md](API/Codecs.md) | `IConfigCodec<T>`、`JsonConfigCodec<T>`、源生成用法与取舍 |
| [API/Registry.md](API/Registry.md) | `ConfigRegistry`、配置节命名、变更事件、DI 扩展 |
