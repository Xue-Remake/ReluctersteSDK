# API：数据库迁移

命名空间：`ReluctersteSDK.Sqlite.Migrations`

版本化迁移：每个迁移有唯一版本号，执行器按版本升序应用未执行的迁移，
并把结果记录到历史表。每个迁移在**独立事务**中执行，失败整体回滚。

入口：

```csharp
using ReluctersteSDK.Sqlite.Migrations;

MigrationResult result = await db.Migrations.MigrateAsync(migrations);
```

---

## 最小示例

```csharp
using ReluctersteSDK.Sqlite;
using ReluctersteSDK.Sqlite.Migrations;

await using var db = new SqliteDataBase(@"E:\Data\App", "app.db");

SqliteMigration[] migrations =
{
    // 迁移 1：按实体特性映射建表（等价于 EnsureTableAsync）
    SqliteMigration.Create(1, "创建 players 表", async (context, ct) =>
    {
        await context.CreateTableIfNotExistsAsync<PlayerRow>(ct);
    }),

    // 迁移 2：加一列
    SqliteMigration.Create(2, "为 players 增加 score", async (context, ct) =>
    {
        await context.AddColumnAsync(
            "players", "score", "INTEGER",
            defaultValueSql: "0",
            cancellationToken: ct);
    })
};

MigrationResult result = await db.Migrations.MigrateAsync(migrations);

Console.WriteLine($"本次执行 {result.AppliedCount} 个，当前版本 {result.CurrentVersion}");
```

第二次运行同一组迁移时，`AppliedCount` 为 0，`SkippedCount` 为 2。

---

## `SqliteMigration`

```csharp
public abstract class SqliteMigration
{
    public abstract long Version { get; }
    public abstract string Description { get; }
    public abstract Task UpAsync(SqliteMigrationContext context, CancellationToken ct = default);
    public virtual Task DownAsync(SqliteMigrationContext context, CancellationToken ct = default);
}
```

| 成员 | 说明 |
|---|---|
| `Version` | 版本号，必须 **大于 0** 且在同一次迁移中唯一 |
| `Description` | 非空说明；写入历史表，也用于异常信息 |
| `UpAsync` | 升级逻辑 |
| `DownAsync` | 回滚逻辑；默认抛 `NotSupportedException`，即“不可回滚” |

### 委托形式

简单迁移不需要子类：

```csharp
SqliteMigration migration = SqliteMigration.Create(
    version: 3,
    description: "给 players 建索引",
    up: async (context, ct) =>
    {
        await context.CreateIndexAsync("ix_players_name", "players", new[] { "name" }, cancellationToken: ct);
    },
    down: async (context, ct) =>
    {
        await context.DropIndexAsync("ix_players_name", ct);
    });
```

`Create` 的 `down` 可省略；省略后该迁移不可回滚。

---

## `SqliteMigrationRunner`

通过 `db.Migrations` 获取，也可以自行创建（例如自定义历史表名）：

```csharp
var runner = new SqliteMigrationRunner(db, "__my_migrations");
```

| 成员 | 说明 |
|---|---|
| `HistoryTableName` | 历史表名，默认 `__schema_migrations` |
| `MigrateAsync(IEnumerable<SqliteMigration>, CancellationToken)` | 应用全部未执行迁移 |
| `RollbackAsync(IEnumerable<SqliteMigration>, long targetVersion = 0, CancellationToken)` | 回滚版本号 **大于** `targetVersion` 的迁移，降序执行 `DownAsync` |
| `GetAppliedMigrationsAsync(CancellationToken)` | 读取历史记录，按版本升序 |

### 历史表

默认结构（表名可用构造函数修改）：

```sql
CREATE TABLE IF NOT EXISTS "__schema_migrations" (
    "version"      INTEGER NOT NULL PRIMARY KEY,
    "description"  TEXT    NOT NULL,
    "applied_at"   TEXT    NOT NULL,   -- ISO 8601（UTC，往返格式）
    "execution_ms" INTEGER NOT NULL
);
```

### 执行规则

1. 迁移集合按 `Version` 升序排序；
2. 已记录的版本直接跳过；
3. 每个迁移在独立事务中执行，成功后写入历史；
4. **不支持乱序迁移**：如果待执行迁移的版本号小于数据库中已有的最大版本，
   直接抛 `InvalidOperationException`（例如已执行 1、3，再补一个 2）；
5. 版本重复、版本号 ≤ 0、说明为空都会在开始执行前抛 `ArgumentException`；
6. 整个执行过程持有数据库写锁，与普通写操作互斥。

### 回滚规则

```csharp
// 回滚到版本 1（即撤销 >1 的所有迁移）
MigrationResult result = await db.Migrations.RollbackAsync(migrations, targetVersion: 1);

// 全部回滚
await db.Migrations.RollbackAsync(migrations);
```

- 待回滚的迁移必须同时出现在 `migrations` 集合中，否则抛 `InvalidOperationException`；
- 未重写 `DownAsync`（且创建时没给 `down`）的迁移回滚时抛 `NotSupportedException`，
  该迁移事务回滚、历史记录保留；
- 回滚按版本**降序**执行。

---

## `MigrationResult` 与 `AppliedMigration`

```csharp
public sealed class MigrationResult
{
    public IReadOnlyList<AppliedMigration> Applied { get; }   // 本次升级的迁移
    public IReadOnlyList<AppliedMigration> Reverted { get; }  // 本次回滚的迁移
    public int AppliedCount { get; }
    public int RevertedCount { get; }
    public int SkippedCount { get; }
    public long PreviousVersion { get; }
    public long CurrentVersion { get; }
    public bool HasChanges { get; }
}
```

`AppliedMigration`：

| 属性 | 说明 |
|---|---|
| `Version` | 版本号 |
| `Description` | 迁移说明 |
| `AppliedAt` | 执行完成时间（UTC） |
| `ExecutionMilliseconds` | `UpAsync` 耗时（毫秒，不含事务提交） |

---

## `SqliteMigrationContext`

上下文绑定当前迁移的连接与事务，所有命令自动挂到该事务。

### 执行与查询

| 方法 | 说明 |
|---|---|
| `ExecuteAsync(sql, parameters = null, ct)` | 执行 DDL/DML，返回受影响行数 |
| `ExecuteScalarAsync(sql, parameters = null, ct)` | 返回首行首列 |
| `QueryAsync<T>(sql, projector, parameters = null, ct)` | 自定义行投影 |

参数形状与 `SqliteDataBase` 一致（匿名对象 / 字典 / `KeyValuePair` 集合 / `SqliteParameter` 集合）。

### 结构探测

| 方法 | 说明 |
|---|---|
| `TableExistsAsync(table, ct)` | 表是否存在（不区分大小写） |
| `ColumnExistsAsync(table, column, ct)` | 列是否存在（不区分大小写） |
| `GetColumnsAsync(table, ct)` | 返回 `SqliteColumnInfo` 列表；表不存在时为空列表 |

`SqliteColumnInfo`：`Name`、`DeclaredType`、`IsNotNull`、`DefaultValue`、`PrimaryKeyOrder`
（0 表示不是主键）。

### DDL 助手

| 方法 | 生成的语句 |
|---|---|
| `CreateTableIfNotExistsAsync<T>()` | 按实体特性映射生成 `CREATE TABLE IF NOT EXISTS` |
| `AddColumnAsync(table, column, type, notNull, defaultValueSql)` | `ALTER TABLE ... ADD COLUMN ...` |
| `RenameColumnAsync(table, old, new)` | `ALTER TABLE ... RENAME COLUMN ... TO ...`（SQLite 3.25+） |
| `DropColumnAsync(table, column)` | `ALTER TABLE ... DROP COLUMN ...`（SQLite 3.35+） |
| `RenameTableAsync(old, new)` | `ALTER TABLE ... RENAME TO ...` |
| `DropTableAsync(table)` | `DROP TABLE IF EXISTS ...` |
| `CreateIndexAsync(index, table, columns, unique, whereSql)` | `CREATE [UNIQUE] INDEX IF NOT EXISTS ...` |
| `DropIndexAsync(index)` | `DROP INDEX IF EXISTS ...` |

表名和列名会自动按标识符规则加双引号并转义；`type`、`defaultValueSql`、`whereSql`
是**原始 SQL 片段**，只应传入代码里写死的值。

```csharp
// 判断后再改，迁移可以安全地处理“部分环境已经手工改过”的情况
if (!await context.ColumnExistsAsync("players", "score", ct))
{
    await context.AddColumnAsync("players", "score", "INTEGER", defaultValueSql: "0", cancellationToken: ct);
}
```

---

## 异常

| 异常 | 场景 |
|---|---|
| `ArgumentException` | 版本重复、说明为空、标识符为空、集合含 null |
| `ArgumentOutOfRangeException` | 版本号 ≤ 0，或回滚目标版本为负 |
| `InvalidOperationException` | 乱序迁移、回滚时缺少迁移定义 |
| `SqliteMigrationException` | 迁移执行失败；含 `Version`、`Description`、`Direction`（Up/Down），原始异常在 `InnerException` |
| `OperationCanceledException` | 取消令牌触发；当前迁移事务回滚 |

`SqliteMigrationException` 的原始异常不会被吞掉：

```csharp
try
{
    await db.Migrations.MigrateAsync(migrations);
}
catch (SqliteMigrationException ex)
{
    Console.WriteLine($"迁移 {ex.Version} 在 {ex.Direction} 阶段失败：{ex.InnerException?.Message}");
}
```

---

## 已知限制

- **不做自动结构比对**：不会根据实体类自动推断差异；新增字段仍需显式写迁移。
- **不支持乱序迁移**：后补的低版本迁移会被拒绝，需要重新编号。
- **没有校验和**：已执行迁移的代码被修改不会被检测；不要修改已发布迁移的语义。
- **迁移内不能切换 `PRAGMA foreign_keys`**：该 PRAGMA 在事务中无效。需要重建表并临时关闭外键时，
  应在迁移之外自行处理，或使用不依赖关闭外键的写法。
- **整体持有写锁**：迁移期间其他写操作等待；大表重建会阻塞较久。
- **回滚依赖 `DownAsync`**：默认不可回滚；生产环境建议为每个迁移写 `DownAsync`。
- **历史表与业务表同库**：迁移不要删除或改写历史表。
