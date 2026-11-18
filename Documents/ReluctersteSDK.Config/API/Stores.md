# API：存储层

命名空间：`ReluctersteSDK.Config.Stores`

- [`IConfigStore<T>`](#iconfigstoret)
- [`ConfigQuery<T>`](#configqueryt)
- [`ConfigSaveResult` / `SaveMode`](#configsaveresult--savemode)
- [`InMemoryConfigStore<T>`](#inmemoryconfigstoret)
- [`JsonFileConfigStore<T>`](#jsonfileconfigstoret)
- [`SqliteConfigStore<T>`](#sqliteconfigstoret)
- [`EntityConfigStore<TEntity, TData>`](#entityconfigstoretentity-tdata)

---

## `IConfigStore<T>`

```csharp
public interface IConfigStore<T> where T : class, IConfig, new()
{
    Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryAsync(
        ConfigQuery<T>? query = null,
        CancellationToken cancellationToken = default);

    Task<ConfigSaveResult> SaveAsync(
        IReadOnlyList<T> items,
        SaveMode mode,
        CancellationToken cancellationToken = default);
}
```

| 成员 | 说明 |
|---|---|
| `GetAsync` | 按主键读取；单主键传一个元素，复合主键按顺序传多列 |
| `QueryAsync` | 谓词查询；`null` 表示全部 |
| `SaveAsync` | 批量保存；返回结果而不是 `bool` |

便捷重载（`ConfigStoreExtensions`）：

```csharp
Task<T?> GetAsync<T>(this IConfigStore<T> store, object key, CancellationToken ct = default);
Task<ConfigSaveResult> SaveAsync<T>(this IConfigStore<T> store, T item, SaveMode mode = SaveMode.Upsert, ...);
Task<ConfigSaveResult> SaveAsync<T>(this IConfigStore<T> store, IReadOnlyList<T> items, SaveMode mode = SaveMode.Upsert, ...);
```

---

## `ConfigQuery<T>`

```csharp
public sealed class ConfigQuery<T>
{
    public Func<T, bool>? Predicate { get; init; }
    public int? Limit { get; init; }
}
```

用**谓词**而不是 SQL 字符串，是三种后端能共用同一套语义的前提。
SQLite 后端会把整表读出来再过滤 —— 配置数据量小，这个代价换来的是一致性。

---

## `ConfigSaveResult` / `SaveMode`

```csharp
public enum SaveMode { Insert, Upsert }

public readonly record struct ConfigSaveResult(bool Success, int Written, Exception? Error);

ConfigSaveResult.Ok(int written);
ConfigSaveResult.Fail(Exception error, int written = 0);
```

| 字段 | 含义 |
|---|---|
| `Success` | 是否全部成功 |
| `Written` | 实际写入条数；批量中途失败时用于判断部分写入 |
| `Error` | 失败原因；成功时为 `null` |

`Insert` 模式在内存与 JSON 后端会**先整体校验**，遇到重复键整批不写。

---

## `InMemoryConfigStore<T>`

```csharp
public InMemoryConfigStore(Func<T, object?> keySelector);
```

字典 + 锁。用于单元测试与原型；主键由 `keySelector` 提供，复合主键可返回元组。

---

## `JsonFileConfigStore<T>`

```csharp
public JsonFileConfigStore(
    FdPath filePath,
    Func<T, object?> keySelector,
    JsonSerializerOptions? options = null);
```

| 特性 | 说明 |
|---|---|
| 文件格式 | 配置数组，默认缩进输出 |
| 写入 | 临时文件 + `File.Move(overwrite)`，不会留下截断文件 |
| 加载 | 首次访问时读入并缓存；文件不存在视为空集 |
| 并发 | 实例内 `SemaphoreSlim` 串行化 |

传 `options` 即可换命名策略或接入 `JsonSerializerContext`（源生成）。

---

## `SqliteConfigStore<T>`

```csharp
public SqliteConfigStore(SqliteDataBase database);
```

| 特性 | 说明 |
|---|---|
| 建表 | 首次访问自动 `EnsureTableAsync<T>()` |
| 主键 | 由 `[DbKey]` 决定，支持复合主键 |
| 批量 | **逐条提交**；ORM 暂无公开事务 API，失败时用 `Written` 判断 |
| 异常 | 捕获后放进 `ConfigSaveResult.Error`，不向调用方抛 |

---

## `EntityConfigStore<TEntity, TData>`

```csharp
public sealed class EntityConfigStore<TEntity, TData>
    where TEntity : class, IEntity<TEntity, TData>
    where TData : class, IData<TEntity>, IConfig, new();

public EntityConfigStore(IConfigStore<TData> store);

Task<TEntity?> GetAsync(IReadOnlyList<object?> keys, CancellationToken ct = default);
Task<IReadOnlyList<TEntity>> QueryAsync(ConfigQuery<TData>? dataQuery = null, CancellationToken ct = default);
Task<ConfigSaveResult> SaveAsync(IReadOnlyList<TEntity> entities, SaveMode mode, CancellationToken ct = default);
Task<ConfigSaveResult> SaveAsync(TEntity entity, SaveMode mode = SaveMode.Upsert, CancellationToken ct = default);
```

把任意 `IConfigStore<TData>` 包成"实体视角"的存储：写入走 `ToData()`，读出走 `ToEntity()`。
物品与库存都不需要单独的管理类。
