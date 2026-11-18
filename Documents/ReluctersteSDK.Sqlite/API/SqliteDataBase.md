# API：SqliteDataBase

命名空间：`ReluctersteSDK.Sqlite`

```csharp
public sealed class SqliteDataBase : IAsyncDisposable
```

一个实例代表"一个数据库文件"。特性的 CRUD 与行模式的底层操作都经过它。

---

## 构造

| 构造 | 说明 |
|---|---|
| `SqliteDataBase(string folderPath, string databaseName, bool enableWal = true)` | `folderPath` 为空抛 `ArgumentException` |
| `SqliteDataBase(FdPath folderPath, string databaseName, bool enableWal = true)` | 先经 `PathAnalyzer` 绝对化 |

构造时会 `Directory.CreateDirectory(folderPath)`，**目录不存在会顺带建出来**。

连接串固定：

```
Data Source = {DatabasePath}
Cache       = Shared
Mode        = ReadWriteCreate
```

| 属性 | 说明 |
|---|---|
| `DatabasePath` | 数据库文件完整路径 |

---

## 初始化

```csharp
public async Task EnsureInitializedAsync()
```

**幂等**，内部用 `Task? _initTask` 缓存，多次调用只真正初始化一次。

实际设置：

| PRAGMA | 值 |
|---|---|
| `busy_timeout` | `5000` |
| `foreign_keys` | `ON` |
| `journal_mode` | `WAL`（`enableWal: true` 时） |
| `synchronous` | `NORMAL`（`enableWal: true` 时） |

> **不调用它也能用**——但那样就没有 WAL 与 `busy_timeout`，并发写会更容易 `SQLITE_BUSY`。

---

## 生命周期

```csharp
public async ValueTask DisposeAsync()
```

只做一件事：等写锁并释放 `SemaphoreSlim`。

- **不关闭连接**：每个操作自己开连接、用完即关，没有长连接可关；
- 之后继续调用方法**不会立刻报错**（写路径会 `ObjectDisposedException`，读路径可能仍然成功）——不要依赖这个行为。

---

## 迁移

```csharp
public SqliteMigrationRunner Migrations { get; }
```

版本化迁移入口。迁移期间持有同一把写锁，与其他写操作互斥；每个迁移独立事务。

```csharp
using ReluctersteSDK.Sqlite.Migrations;

MigrationResult result = await db.Migrations.MigrateAsync(new SqliteMigration[]
{
    SqliteMigration.Create(1, "初始化", async (context, ct) =>
    {
        await context.CreateTableIfNotExistsAsync<PlayerRow>(ct);
    })
});
```

方法、历史表结构与限制见 [Migrations.md](Migrations.md)。

---

## 原生 SQL

| 方法 | 返回 | 说明 |
|---|---|---|
| `ExecuteAsync(string sql, object? parameters = null)` | `int` | 受影响行数；走写锁 |
| `ExecuteScalarAsync(string sql, object? parameters = null)` | `object?` | 首行首列 |
| `QuerySqlAsync<TResult>(string sql, Func<SqliteDataReader, TResult> projector, object? parameters = null)` | `List<TResult>` | 自定义行投影 |

### `parameters` 支持的形状

| 形状 | 例子 |
|---|---|
| 匿名对象 | `new { id = 1, name = "a" }` → `@id` / `@name` |
| `Dictionary<string, object?>` | |
| `IEnumerable<KeyValuePair<string, object?>>` | |
| `IEnumerable<SqliteParameter>` | 直接塞进命令 |

值统一经 `SqliteTypeMapper.ToDbValue` 转换（枚举 → 整数、`bool` → 0/1、`DateTime` → `"O"` 字符串）。

---

## 类型映射

| .NET 类型 | SQLite 类型 | 存储形式 |
|---|---|---|
| `byte` `sbyte` `short` `ushort` `int` `uint` `long` `ulong` `bool` | `INTEGER` | `bool` → 0/1 |
| `enum`（任意底层类型） | `INTEGER` | 底层整数值 |
| `float` `double` `decimal` | `REAL` | |
| `string` | `TEXT` | |
| `DateTime` | `TEXT` | `"O"` 往返格式（含 `Kind`） |
| `DateTimeOffset` | `TEXT` | `"O"` |
| `Guid` | `TEXT` | `value.ToString()` |
| `byte[]` | `BLOB` | |
| `Nullable<T>` | 同 `T` | `DBNull` ↔ `null` |

`SqliteTypeMapper.ToSqliteType` 遇到其它类型（自定义类、结构体）直接抛 `NotSupportedException`，**在建表阶段就会失败**。

读回时：`DBNull` → 引用类型得 `null`、值类型得 `default`；枚举按 `Int64` 转回、`decimal` 显式转换、其余走 `Convert.ChangeType`。

---

## 并发模型

- **写操作串行化**：`Insert` / `Upsert` / `Update` / `Delete` / `ExecuteAsync` / `EnsureTableAsync` 全部经过 `SemaphoreSlim(1,1)`。
- **读操作不进锁**：依赖 WAL + `busy_timeout=5000` 与写并发。
- **每次操作新建 `SqliteConnection`**（`Cache=Shared`），用完即关，**没有连接池复用**。

> 高频写会同时遇到"每次新建连接"和"全局写锁"两个瓶颈。批量插入请用 `RowTable.AddRangeAsync`（单事务）或自己开事务。

---

## 已知限制

- **没有事务 API**：特性映射模式无法跨多次操作开事务；RowMod 的 `AddRangeAsync` 内部有事务但不对外暴露。
- **`Cache=Shared` + WAL 组合**：共享缓存会削弱 WAL 的部分并发优势，且多连接共享缓存影响写入吞吐。
- **`ExecuteAsync` 的 `sql` 是裸字符串**：只接受自己代码里写死的 SQL，**永远不要拼用户输入**（值请走 `parameters`）。
- **没有重试**：`SQLITE_BUSY` 只会等 `busy_timeout`，超时后抛异常。
- **`DisposeAsync` 不释放连接**：没有连接可释放，但仍应 `await using` 以释放信号量。
- **不支持自定义连接串**：`Cache`、`Mode` 写死。
- **建表只做 `CREATE TABLE IF NOT EXISTS`**：不会自动比对实体与已有表结构；结构演进请使用内置迁移（[Migrations.md](Migrations.md)），迁移本身也不会做自动差异推断。
