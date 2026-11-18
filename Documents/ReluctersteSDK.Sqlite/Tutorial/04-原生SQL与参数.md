# 04 · 原生 SQL 与参数

**目标**：在用特性映射或 RowMod 都别扭时，直接写 SQL：执行 DML、取标量、把结果投影成任意类型，并正确绑定参数。

---

## 1. 三个入口

```csharp
using ReluctersteSDK.Sqlite;

// 非查询：返回受影响行数
int affected = await db.ExecuteAsync(
    "UPDATE players SET score = score + 1 WHERE score < @max",
    new { max = 100 });

// 标量：首行首列
object? count = await db.ExecuteScalarAsync("SELECT COUNT(*) FROM players");

// 自定义投影：每行交给委托
List<string> names = await db.QuerySqlAsync(
    "SELECT name FROM players ORDER BY score DESC",
    reader => reader.GetString(0));
```

| 方法 | 连接/锁 | 用途 |
|---|---|---|
| `ExecuteAsync` | 写锁 | `INSERT` / `UPDATE` / `DELETE` / DDL |
| `ExecuteScalarAsync` | 不加锁 | `COUNT`、`MAX`、`EXISTS`、`PRAGMA` 读取 |
| `QuerySqlAsync` | 不加锁 | 任意 `SELECT` + 自定义映射 |

`ExecuteScalarAsync` 与 `QuerySqlAsync` 走读路径，不加写锁；`ExecuteAsync` 走写锁，与其它写操作互斥。

---

## 2. 参数形状

四个入口都接受同一个 `parameters` 对象：

```csharp
// 1) 匿名对象
new { id = 1, name = "Alice" }

// 2) Dictionary<string, object?>
new Dictionary<string, object?>
{
    ["id"] = 1,
    ["name"] = "Alice"
}

// 3) KeyValuePair 集合
new[]
{
    new KeyValuePair<string, object?>("id", 1),
    new KeyValuePair<string, object?>("name", "Alice")
}

// 4) SqliteParameter 集合
new[] { new SqliteParameter("@id", 1) }
```

- 参数名不需要带 `@`，内部会补；
- 匿名对象的属性名就是参数名；
- 值统一经 `SqliteTypeMapper.ToDbValue`：枚举→整数、`bool`→0/1、`DateTime`→`"O"` 字符串、`Guid`→字符串。

```csharp
await db.ExecuteAsync(
    "INSERT INTO events (id, kind, created) VALUES (@id, @kind, @created)",
    new
    {
        id = 1,
        kind = EventKind.Combat,        // enum → INTEGER
        created = DateTime.UtcNow       // → "O" 文本
    });
```

---

## 3. 查询与投影

`QuerySqlAsync<TResult>` 的委托接收 `SqliteDataReader`：

```csharp
using Microsoft.Data.Sqlite;

public record PlayerSummary(string Name, int Score);

List<PlayerSummary> summaries = await db.QuerySqlAsync(
    "SELECT name, score FROM players WHERE score >= @min ORDER BY score DESC",
    reader => new PlayerSummary(
        reader.GetString(0),
        reader.GetInt32(1)),
    new { min = 10 });
```

也可以用列名取值：

```csharp
reader.GetInt64(reader.GetOrdinal("id"))
```

### 多表 JOIN

```csharp
var rows = await db.QuerySqlAsync(
    """
    SELECT p.name, i.item_id, i.count
    FROM players p
    JOIN inventory i ON i.player_id = p.id
    WHERE p.id = @playerId
    """,
    r => new
    {
        Name = r.GetString(0),
        ItemId = r.GetInt64(1),
        Count = r.GetInt32(2)
    },
    new { playerId = 1001 });
```

### 分组与聚合

```csharp
var stats = await db.QuerySqlAsync(
    "SELECT player_id, SUM(count) AS total FROM inventory GROUP BY player_id",
    r => (PlayerId: r.GetInt64(0), Total: r.GetInt64(1)));
```

---

## 4. 标量查询

```csharp
object? raw = await db.ExecuteScalarAsync("SELECT COUNT(*) FROM players");
long count = Convert.ToInt64(raw);

object? maxScore = await db.ExecuteScalarAsync(
    "SELECT MAX(score) FROM players WHERE name LIKE @p",
    new { p = "A%" });

// 没有行时 MAX 返回 DBNull
if (maxScore is DBNull || maxScore is null)
    Console.WriteLine("没有匹配行");
```

`ExecuteScalarAsync` **不会**帮你做类型转换，返回值是 `object?`（通常是 `long` / `double` / `string` / `byte[]` / `DBNull`）。

---

## 5. 常用 DDL

```csharp
await db.ExecuteAsync("""
    CREATE INDEX IF NOT EXISTS ix_players_score ON players(score);
    """);

await db.ExecuteAsync("""
    ALTER TABLE players ADD COLUMN nickname TEXT;
    """);

await db.ExecuteAsync("DROP TABLE IF EXISTS temp_import;");
```

> SQLite 的 `ALTER TABLE ADD COLUMN` 不支持一次加多列；复杂结构变更建议走迁移（见 [07-数据库迁移.md](07-数据库迁移.md)），因为迁移自带事务与历史记录。

---

## 6. 事务的限制

`ExecuteAsync` 每次调用是**独立连接 + 独立语句**，没有对外事务：

```csharp
// ❌ 不是原子操作
await db.ExecuteAsync("UPDATE accounts SET balance = balance - 100 WHERE id = 1");
await db.ExecuteAsync("UPDATE accounts SET balance = balance + 100 WHERE id = 2");
// 第二条失败时，第一条已经提交
```

需要原子性时：

1. 用**一条 SQL** 完成（`UPDATE … CASE`、`INSERT … SELECT`）；
2. 用迁移上下文（`SqliteMigrationContext` 绑定单个事务）；
3. 用 RowMod 的 `AddRangeAsync`（内部单事务，但只做插入）。

库没有暴露 `BeginTransactionAsync`，这是当前版本的明确限制。

---

## 7. 注入与转义

```csharp
// ❌ 绝对不要这样
string sql = $"SELECT * FROM players WHERE name = '{userInput}'";

// ✅ 值永远是参数
await db.QuerySqlAsync(
    "SELECT * FROM players WHERE name = @name",
    r => r.GetString(0),
    new { name = userInput });
```

| 部分 | 是否可信 |
|---|---|
| 表名 / 列名 / `ORDER BY` 片段 | 必须来自代码常量 |
| `WHERE` 里的值 | 必须走 `parameters` |
| `LIMIT` / `OFFSET` | 库内部拼接；自己写时用整数 |

`SqliteUtils.QuoteIdentifier` 只给**标识符**加引号，不处理值。

---

## 8. 完整示例：导入并汇总

```csharp
using Microsoft.Data.Sqlite;
using ReluctersteSDK.Sqlite;

await using var db = new SqliteDataBase(folder, "import.db");
await db.EnsureInitializedAsync();

await db.ExecuteAsync("""
    CREATE TABLE IF NOT EXISTS raw_events (
        id       INTEGER PRIMARY KEY,
        player   TEXT    NOT NULL,
        kind     TEXT    NOT NULL,
        amount   INTEGER NOT NULL,
        created  TEXT    NOT NULL
    );
    """);

// 逐条写入（生产环境量大时考虑批量）
for (int i = 1; i <= 100; i++)
{
    await db.ExecuteAsync(
        "INSERT INTO raw_events (id, player, kind, amount, created) VALUES (@id, @p, @k, @a, @c)",
        new
        {
            id = i,
            p = $"player{i % 7}",
            k = i % 2 == 0 ? "kill" : "death",
            a = i * 3,
            c = DateTime.UtcNow
        });
}

// 汇总
var summary = await db.QuerySqlAsync(
    """
    SELECT player, kind, COUNT(*) AS n, SUM(amount) AS total
    FROM raw_events
    WHERE created >= @since
    GROUP BY player, kind
    ORDER BY player, kind
    """,
    r => new
    {
        Player = r.GetString(0),
        Kind = r.GetString(1),
        Count = r.GetInt64(2),
        Total = r.GetInt64(3)
    },
    new { since = DateTime.UtcNow.AddDays(-1) });

foreach (var row in summary)
    Console.WriteLine($"{row.Player,-10} {row.Kind,-6} {row.Count,4} {row.Total,8}");
```

---

## 9. 限制

- 没有事务 API；
- 没有 `CancellationToken`；
- 没有连接复用：每次调用新建连接；
- `QuerySqlAsync` 的投影委托里不要做重活（逐行同步回调）；
- SQL 片段不校验；拼错要到执行期才报错。

---

## 下一步

- 动态表、不定义实体 → [05-纯行模式.md](05-纯行模式.md)
- 需要事务/版本化结构变更 → [07-数据库迁移.md](07-数据库迁移.md)
