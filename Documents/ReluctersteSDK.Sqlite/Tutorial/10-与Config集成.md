# 10 · 与 `ReluctersteSDK.Config` 集成

**目标**：把 SQLite 当作 `IConfigStore<T>` 的后端，用统一的配置读写接口；以及把业务实体与数据行分开时怎么接 `EntityConfigStore`。

> 本章需要额外引用 `ReluctersteSDK.Config`。只用 ORM 时不需要。

---

## 1. 直接存配置

```xml
<PackageReference Include="ReluctersteSDK.Config" Version="0.13.0.0" />
```

```csharp
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Sqlite;

[DbTable("settings")]
public sealed class GameSetting : IConfig
{
    [DbKey("key")]    public string Key { get; set; } = "";
    [DbColumn("val")] public string Value { get; set; } = "";
}

await using var db = new SqliteDataBase(folder, "game.db");
await db.EnsureInitializedAsync();

var store = new SqliteConfigStore<GameSetting>(db);

// 保存（默认 Upsert）
ConfigSaveResult saved = await store.SaveAsync(new GameSetting
{
    Key = "volume",
    Value = "80"
});

if (!saved.Success)
    Console.WriteLine($"写入失败：{saved.Error?.Message}");

// 按主键读取
GameSetting? setting = await store.GetAsync("volume");

// 谓词查询（SQLite 后端会整表读出再过滤）
IReadOnlyList<GameSetting> uiSettings = await store.QueryAsync(
    new ConfigQuery<GameSetting>
    {
        Predicate = s => s.Key.StartsWith("ui.", StringComparison.Ordinal)
    });
```

要点：

- `SqliteConfigStore<T>` 首次访问自动 `EnsureTableAsync<T>()`；
- 主键来自 `[DbKey]`，支持复合主键（`GetAsync` 传多列）；
- 批量保存是**逐条提交**，失败时用 `ConfigSaveResult.Written` 判断写到了第几条；
- 异常被收进 `ConfigSaveResult.Error`，不向调用方抛。

### `SaveMode`

```csharp
// 覆盖或插入
await store.SaveAsync(item, SaveMode.Upsert);

// 只插入；重复键失败
ConfigSaveResult inserted = await store.SaveAsync(item, SaveMode.Insert);

// 批量
ConfigSaveResult batch = await store.SaveAsync(items, SaveMode.Insert);
```

---

## 2. 业务实体 ↔ 数据行

当内存模型需要行为、落库结构需要扁平字段时，用 `EntityConfigStore<TEntity, TData>`：

```csharp
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Sqlite;

// 数据行：标 ORM 特性
[DbTable("items")]
public sealed class ItemDataRow : IItemData<ItemEntity>
{
    [DbKey("item_id")]   public int ItemId { get; set; }
    [DbColumn("name")]   public string Name { get; set; } = "";
    [DbColumn("rarity")] public int Rarity { get; set; }

    public ItemEntity ToEntity() => new ItemEntity
    {
        ItemId = ItemId,
        Name = Name,
        Rarity = Rarity
    };
}

// 业务模型：不放 ORM 特性
public sealed class ItemEntity : IItemEntity<ItemEntity, ItemDataRow>
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public int Rarity { get; set; }

    public ItemDataRow ToData() => new ItemDataRow
    {
        ItemId = ItemId,
        Name = Name,
        Rarity = Rarity
    };
}
```

接入：

```csharp
var inner = new SqliteConfigStore<ItemDataRow>(db);
var items = new EntityConfigStore<ItemEntity, ItemDataRow>(inner);

await items.SaveAsync(new ItemEntity { ItemId = 1001, Name = "Sword", Rarity = 3 });

ItemEntity? one = await items.GetAsync(new object?[] { 1001 });

IReadOnlyList<ItemEntity> rare = await items.QueryAsync(
    new ConfigQuery<ItemDataRow>
    {
        Predicate = d => d.Rarity >= 3
    });
```

**ORM 特性只标在 `TData` 上**，`TEntity` 不参与映射。

---

## 3. 什么时候值得用

| 场景 | 建议 |
|---|---|
| 只有一张配置表、读写简单 | 直接 `SqliteDataBase` + `EnsureTableAsync` |
| 想以后换内存/JSON 后端 | `IConfigStore<T>` + `SqliteConfigStore<T>` |
| 业务模型与落库结构不同 | `EntityConfigStore` |
| 需要 SQL 查询能力 | 直接用 ORM；`ConfigQuery` 只有谓词 |

`Config` 的 `ConfigQuery` 是**谓词**，不是 SQL；SQLite 后端为了三后端语义一致，会把整表读出来再过滤。配置数据量小可以接受，大表请直接用 `QueryAsync<T>` 写 SQL 条件。

---

## 4. 完整示例：配置存储

```csharp
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Sqlite;

public static class SettingsStore
{
    public static async Task RunAsync(string folder)
    {
        await using var db = new SqliteDataBase(folder, "settings.db");
        await db.EnsureInitializedAsync();

        var store = new SqliteConfigStore<GameSetting>(db);

        ConfigSaveResult saved = await store.SaveAsync(new[]
        {
            new GameSetting { Key = "volume", Value = "80" },
            new GameSetting { Key = "ui.scale", Value = "1.25" },
            new GameSetting { Key = "ui.theme", Value = "dark" }
        });

        Console.WriteLine($"写入 {saved.Written} 条，成功={saved.Success}");

        IReadOnlyList<GameSetting> ui = await store.QueryAsync(
            new ConfigQuery<GameSetting> { Predicate = s => s.Key.StartsWith("ui.") });

        foreach (GameSetting s in ui)
            Console.WriteLine($"{s.Key} = {s.Value}");
    }
}
```

---

## 5. 限制

- `SqliteConfigStore` 批量不是事务；
- `ConfigQuery` 只有谓词与 `Limit`，没有排序；
- `IConfigStore<T>` 要求 `class, IConfig, new()`；
- `EntityConfigStore` 的 `TEntity` 必须实现对应的 `IItemEntity` / `IInventoryEntity` 契约；
- 配置类型改名 / 改表名不会自动迁移，需要自己写 `SqliteMigration`。

---

## 回到目录

- [Tutorial 目录](README.md)
- [Config 文档](../../ReluctersteSDK.Config/README.md)
- [API/Stores.md](../../ReluctersteSDK.Config/API/Stores.md)
