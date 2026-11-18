# API：纯行模式（RowMod）

命名空间：`ReluctersteSDK.Sqlite.RowMod`

不定义实体类，直接按"表名 + 行字典"操作。

| 类型 | 角色 |
|---|---|
| `RowDbContext` | 表访问入口，负责释放 |
| `RowTable` | 单表操作 |
| `RowQuery` | 查询构建器（链式） |
| `Row` | 一行的数据（列名字典） |

---

## `RowDbContext`

```csharp
public class RowDbContext : IAsyncDisposable
{
    public SqliteDataBase Database { get; }
    public RowDbContext(SqliteDataBase db);
    public RowTable Table(string tableName);
    public RowTable this[string tableName] { get; }
    public ValueTask DisposeAsync();
}
```

`DisposeAsync` 会调用 `Database.DisposeAsync()`——**所以不要对 `db` 和 `ctx` 同时 `await using`**：

```csharp
await using var ctx = new RowDbContext(db);   // ✅ 只释放 ctx
```

---

## `RowTable`

| 成员 | 说明 |
|---|---|
| `TableName` | 表名 |
| `CreateTableIfNotExistsAsync(params (string ColumnName, string ColumnType)[] columns)` | 建表；列类型写原始 SQLite 类型字符串 |
| `AddAsync(Row row)` | 插入单行 |
| `AddRangeAsync(IEnumerable<Row> rows)` | **单事务**批量插入 |
| `Where(Expression<Func<Row,bool>>)` | 返回 `RowQuery` |
| `Select(params string[])` | 返回 `RowQuery` |
| `ToListAsync()` / `CountAsync()` | 全表查询 |

```csharp
await users.CreateTableIfNotExistsAsync(
    ("id",    "INTEGER PRIMARY KEY AUTOINCREMENT"),
    ("name",  "TEXT NOT NULL"),
    ("age",   "INTEGER"));
```

列类型是**裸字符串**，直接拼进 `CREATE TABLE`——只接受代码里写死的值。

`AddRangeAsync` 用一条 `INSERT` 多 `VALUES` 的方式并包在事务里，比循环 `AddAsync` 快很多。

---

## `RowQuery`

全部方法返回 `this`，可以链式调用。

| 方法 | 生成的 SQL 片段 |
|---|---|
| `Select(params string[] columns)` | `SELECT c1, c2`（不调用则为 `*`） |
| `Where(Expression<Func<Row,bool>>)` | `WHERE ...`，多次调用用 `AND` 连接 |
| `OrderBy(string column)` | `ORDER BY "col" ASC` |
| `OrderByDescending(string column)` | `ORDER BY "col" DESC` |
| `Skip(int count)` | `OFFSET n`；单独使用时生成 `LIMIT -1 OFFSET n` |
| `Take(int count)` | `LIMIT n` |
| `ToListAsync()` | `List<Row>` |
| `FirstOrDefaultAsync()` | `Row?` |
| `CountAsync()` | `long` |
| `DeleteAsync()` | `int` 受影响行数 |
| `UpdateAsync(Row updateData)` | `int` 受影响行数 |

> 只设 `Skip` 不设 `Take` 时，生成 `LIMIT -1 OFFSET n`（SQLite 中 `LIMIT -1` 表示不限）。

---

## `Row`

```csharp
public sealed class Row : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
```

| 成员 | 说明 |
|---|---|
| `this[string columnName]` | 读写；**列名忽略大小写**；列不存在时读返回 `null` |
| `Get<T>(string columnName)` | 走 ORM 的类型转换；值缺失或 `DBNull` 返回 `default` |
| `Keys` / `Values` / `Count` | 来自内部字典（`OrdinalIgnoreCase`） |
| `Add` / `Remove` / `Clear` / `ContainsKey` / `TryGetValue` | 标准字典行为 |

```csharp
var row = new Row { ["Name"] = "Alice", ["age"] = 25 };
row["name"];              // "Alice"   大小写不敏感
row["missing"];           // null      不抛异常

row.Get<int>("age");      // 25
row.Get<string>("name");  // "Alice"
```

`Get<T>` 的转换规则与[类型映射](SqliteDataBase.md#类型映射)一致：枚举按整数、`bool` 按 0/1、`DateTime` 按 `"O"` 格式解析。

---

## 表达式支持范围

`Where(Expression<Func<Row,bool>>)` 由 `ParseExpression` 翻译成 SQL，**只支持**：

| 类别 | 支持 |
|---|---|
| 比较 | `==` `!=` `>` `>=` `<` `<=` |
| 逻辑 | `&&` `\|\|` `!` |
| 取列 | `r.Get<T>("col")`、`r["col"]` |
| 字符串 | `string.Contains("x")` → `LIKE '%x%'` |
| 常量 / 变量 | 作为参数绑定 |

**不支持**：`in`、`StartsWith` / `EndsWith`、排序 lambda、`OR` 分组括号、方法调用（除 `Contains`）、字符串插值。

```csharp
await users
    .Where(r => r.Get<int>("age") >= 18 && r.Get<string>("email")!.Contains("gmail"))
    .OrderByDescending("age")
    .Skip(10)
    .Take(2)
    .ToListAsync();
```

### 其它表达式怎么办

不认识的子表达式会走 `Expression.Compile()` **在内存里求值**，结果作为参数绑定。这意味着：

- 能跑通，但被求值的部分**不参与 SQL 过滤**（性能上等于全表扫一遍再筛）；
- 引用 `Row` 实例的表达式在编译时可能直接抛异常。

复杂查询请直接写 SQL（`db.QuerySqlAsync`）。

---

## 已知限制

- **表达式不校验列名**：拼错要到 SQLite 执行时才报错。
- **没有 `JOIN` / 子查询 / 分组**：只有单表 + `WHERE` / `ORDER BY` / `LIMIT` / `OFFSET` / `SELECT`。
- **`UpdateAsync(Row)` 是整行更新**：`updateData` 里的列都会写进去，没给的列保持不变。
- **`Row` 的列名忽略大小写**，但 `Select` / `OrderBy` / 表达式里的列名字符串原样进 SQL。
- **`RowQuery` 是可变构建器**：重复执行同一个实例会累积 `WHERE` 条件；`FirstOrDefaultAsync()` 还会把 `_limit` 永久改成 1，之后同一个实例再 `ToListAsync()` 只会返回一行。
- **`RowDbContext.DisposeAsync` 连带释放 `SqliteDataBase`**：不要对两者都 `await using`。
- **`Skip` 单独使用**依赖 `LIMIT -1` 这个 SQLite 方言，换数据库会失效。
