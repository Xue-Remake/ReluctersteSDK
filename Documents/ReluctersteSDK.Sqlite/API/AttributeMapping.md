# API：特性映射模式

命名空间：`ReluctersteSDK.Sqlite`

把 POCO 通过特性映射到表，提供强类型 CRUD。

---

## 四个特性

| 特性 | 目标 | 说明 |
|---|---|---|
| `[DbTable("name")]` | class | 表名；不写或为空时用**类名**（不是小写、不是复数） |
| `[DbKey("id")]` | property | 主键；可标多个（复合主键） |
| `[DbColumn("name")]` | property | 普通列 |
| `[DbIgnore]` | property | 显式排除 |

### 列名解析优先级

```
[DbColumn.Name] > [DbKey.Name] > 属性名
```

`[DbColumn]` / `[DbKey]` 不写参数时直接用属性名。

### 哪些属性会入库

```csharp
type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.CanRead && p.CanWrite)          // 必须可读写
    .Where(p => p.GetCustomAttribute<DbIgnoreAttribute>() == null)
    // 然后只保留有 [DbKey] 或 [DbColumn] 的
```

三条规则：

1. `[DbKey]` 属性**一定**入库；
2. `[DbColumn]` 属性入库；
3. 两者都没有的属性**默认不入库**（不需要 `[DbIgnore]`）。

**一个映射都没有时抛 `InvalidOperationException`**（建表前就会失败）。

> 属性必须是 `public` 且同时有 `get` / `set`。只读属性（只有 `get`）会被静默跳过。

### 映射缓存

`EntityMapCache` 用 `ConcurrentDictionary<Type, EntityMap>` 缓存，**每种类型只反射一次**。因此运行期改特性不会生效。

### 可空性

`IsNullable` 规则：

- 引用类型 → `true`
- `Nullable<T>` → `true`
- 其它值类型 → `false`

---

## CRUD 方法

全部在 `SqliteDataBase` 上，全部 `async`。

| 方法 | 返回 | 说明 |
|---|---|---|
| `EnsureTableAsync<T>()` | `Task` | `CREATE TABLE IF NOT EXISTS`；`where T : new()` |
| `InsertAsync<T>(T entity)` | `Task<int>` | 受影响行数 |
| `UpsertAsync<T>(T entity)` | `Task<int>` | `ON CONFLICT DO UPDATE` |
| `UpdateAsync<T>(T entity)` | `Task<int>` | 按主键更新**非键列** |
| `DeleteAsync<T>(T entity)` | `Task<int>` | 按主键删除 |
| `DeleteByKeyAsync<T>(params object?[] keys)` | `Task<int>` | |
| `GetByKeyAsync<T>(params object?[] keys)` | `Task<T?>` | 找不到返回 `null` |
| `QueryAsync<T>(string? where = null, object? parameters = null, string? orderBy = null, int? limit = null)` | `Task<List<T>>` | `where T : new()` |
| `GetAllAsync<T>()` | `Task<List<T>>` | 顺带 `EnsureTableAsync` |

### 主键顺序

多主键时，`GetByKeyAsync<T>(params object?[] keys)` 的**参数顺序必须与 `[DbKey]` 在属性声明中的顺序一致**。

```csharp
[DbKey("a")] public int A { get; set; }   // 第 1 个
[DbKey("b")] public int B { get; set; }   // 第 2 个

await db.GetByKeyAsync<Row>(1, 2);        // a=1, b=2 ✅
await db.GetByKeyAsync<Row>(2, 1);        // ❌
```

### `where` 片段

- 可带可不带 `WHERE` 关键字（内部会检测）；
- **片段本身不转义**，只接受代码里写死的字符串；
- 值请走 `parameters`（匿名对象 / 字典 / `KeyValuePair` 集合 / `SqliteParameter` 集合）。

```csharp
await db.QueryAsync<PlayerRow>(
    where: "score > @min AND name LIKE @prefix",
    parameters: new { min = 15, prefix = "A%" },
    orderBy: "score DESC",
    limit: 10);
```

`orderBy` 同样是裸字符串。

### `UpdateAsync` 的行为

- 用主键定位；
- 更新**全部非键列**（即使值没变）；
- 没有非键列时直接返回 `0`，不执行 SQL。

### `UpsertAsync` 的行为

`INSERT ... ON CONFLICT(主键) DO UPDATE SET 非键列 = excluded.非键列`。

### `QueryAsync` 的类型约束

```csharp
where T : new()
```

实体必须有**公共无参构造函数**，否则编译不过。

---

## 已知限制

- **建表不迁移**：`EnsureTableAsync` 只做 `CREATE TABLE IF NOT EXISTS`，字段增删不会同步；请使用 [迁移](Migrations.md) 显式管理结构变更（迁移支持 `CreateTableIfNotExistsAsync<T>()`，但同样不会自动比对差异）。
- **没有事务 API**：跨多次 `Insert` / `Update` 不构成原子操作。
- **`UpdateAsync` 覆盖所有非键列**：想只改一列得自己写 `ExecuteAsync`。
- **列名 / 表名不转义用户输入**：全部来自特性，静态可信。
- **不支持继承映射**：只扫描声明类型自身的 `public` 实例属性（含继承来的，但表结构不会分层）。
- **不支持复杂类型**：`List<>`、自定义类会在建表阶段抛 `NotSupportedException`。
- **映射缓存在进程内**：同进程多个 `SqliteDataBase` 共享，类型改不了。
