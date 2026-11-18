# ConsoleApp1（示例工程）

`ReluctersteSDK.Sqlite` 纯行模式（RowMod）的完整可运行示例。

- 类型：`Exe` 示例工程，不打包
- 命名空间：`ConsoleApp1`
- 依赖：`ReluctersteSDK`（聚合包）

## 运行

```powershell
dotnet run --project ConsoleApp1/ConsoleApp1.csproj
```

会在 `bin/Debug/net10.0/Data/Databases/rowmod_demo.db` 建库并跑完 8 个步骤。

## 示例覆盖的 8 步

| 步骤 | 演示内容 |
|---|---|
| 1 | `PhysicalPathHelper.EnsureDirectoryExists` + `CreateTableIfNotExistsAsync` |
| 2 | `AddAsync` 插入单行 |
| 3 | `AddRangeAsync` 单事务批量插入 |
| 4 | `CountAsync` / `ToListAsync` 全量查询 |
| 5 | 表达式 `Where` + `OrderByDescending` + `Take` |
| 6 | `FirstOrDefaultAsync` |
| 7 | `Where(...).UpdateAsync(Row)` |
| 8 | `Where(...).DeleteAsync()` |

## 关键代码

```csharp
using ReluctersteSDK.Paths;
using ReluctersteSDK.Paths.Tools;
using ReluctersteSDK.Sqlite;
using ReluctersteSDK.Sqlite.RowMod;

// 1. 用 Paths 确保目录存在
AbsolutePath? dbFolder = PhysicalPathHelper.EnsureDirectoryExists(
    new RelativePath(@"Data\Databases"));

// 2. 建库
var sqliteDb = new SqliteDataBase(dbFolder!.PathStr, "rowmod_demo.db");
await sqliteDb.EnsureInitializedAsync();

// 3. 由 RowDbContext 统一负责释放（DisposeAsync 会连带释放 SqliteDataBase）
await using var dbContext = new RowDbContext(sqliteDb);

RowTable users = dbContext["users"];

await users.CreateTableIfNotExistsAsync(
    ("id",        "INTEGER PRIMARY KEY AUTOINCREMENT"),
    ("name",      "TEXT NOT NULL"),
    ("age",       "INTEGER"),
    ("email",     "TEXT"),
    ("is_active", "INTEGER"));

await users.AddAsync(new Row
{
    ["name"] = "Alice",
    ["age"] = 25,
    ["email"] = "alice@example.com",
    ["is_active"] = 1
});

var result = await users
    .Where(r => r.Get<int>("age") >= 18 && r.Get<string>("email")!.Contains("gmail"))
    .OrderByDescending("age")
    .Take(2)
    .ToListAsync();
```

## 值得注意的两点

1. **不要同时 `await using` 两个对象**。`RowDbContext.DisposeAsync()` 内部会调用 `Database.DisposeAsync()`，
   所以只 `await using` 掉 `dbContext` 即可，`sqliteDb` 不要再套一层。

2. **`Row` 的列名忽略大小写**，`row["Name"]` 与 `row["name"]` 是同一列。
   但 `RowQuery` 生成 SQL 时的列名取自表达式里的字符串字面量，写错了要到执行期才报错。

更完整的 RowMod 说明见 [API/RowMod.md](../ReluctersteSDK.Sqlite/API/RowMod.md)。
