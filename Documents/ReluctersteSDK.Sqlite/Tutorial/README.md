# ReluctersteSDK.Sqlite Tutorial

从建库到迁移、并发与排错的**完整用法示例**。每个章节都可以独立阅读；示例默认是控制台程序，直接抄进工程即可运行。

> 包的整体设计、能力总览与取舍看 [../README.md](../README.md)；
> 方法签名与边界看 [../API/](../API/)。

---

## 阅读顺序

| # | 章节 | 你会得到 |
|---|---|---|
| 01 | [快速开始](01-快速开始.md) | 一个完整控制台程序：建库、建表、增删改查、释放 |
| 02 | [实体映射](02-实体映射.md) | 四个特性、列名规则、复合主键、可空性、类型映射 |
| 03 | [增删改查](03-增删改查.md) | 全部 CRUD 方法、条件与参数、排序分页、Upsert/Update 差异 |
| 04 | [原生 SQL 与参数](04-原生SQL与参数.md) | `ExecuteAsync` / `ExecuteScalarAsync` / `QuerySqlAsync` 与参数形状 |
| 05 | [纯行模式 RowMod](05-纯行模式.md) | 不定义实体类，直接按表名 + 行字典操作 |
| 06 | [表达式查询](06-表达式查询.md) | `Where(lambda)` 的翻译范围、链式构建与坑 |
| 07 | [数据库迁移](07-数据库迁移.md) | 版本化迁移、DDL 助手、历史表、回滚、乱序校验 |
| 08 | [并发、事务与性能](08-并发事务与性能.md) | 写锁、连接模型、批量插入、WAL 与调优 |
| 09 | [常见问题与排错](09-常见问题与排错.md) | 异常对照表、限制清单、排查路径 |
| 10 | [与 Config 集成](10-与Config集成.md) | `SqliteConfigStore<T>` 与 `EntityConfigStore` 的完整接法 |

第一次使用按 01 → 02 → 03 走；需要动态 schema 看 05/06；上线前补 07/08。

---

## 前置条件

- .NET SDK **10.0**；
- 引用 `ReluctersteSDK.Sqlite`：

```xml
<PackageReference Include="ReluctersteSDK.Sqlite" Version="0.13.0.0" />
```

`Microsoft.Data.Sqlite` 会作为传递依赖引入；需要显式控制版本时也可以自己再引用一次。

---

## 示例约定

- 数据库文件放在 `AppContext.BaseDirectory/Data`，避免污染工作目录；
- 所有示例用 `await using` 释放 `SqliteDataBase`；
- `where` / `orderBy` / 列类型都是**代码里写死的 SQL 片段**；值一律走 `parameters`；
- `QueryAsync<T>` / `GetByKeyAsync<T>` 等要求实体有**公共无参构造函数**（`where T : new()`）。

---

## 一页速查

```csharp
using ReluctersteSDK.Sqlite;

await using var db = new SqliteDataBase(folder, "app.db");
await db.EnsureInitializedAsync();

await db.EnsureTableAsync<PlayerRow>();
await db.InsertAsync(new PlayerRow { Id = 1, Name = "A", Score = 10 });

PlayerRow? one = await db.GetByKeyAsync<PlayerRow>(1);

List<PlayerRow> top = await db.QueryAsync<PlayerRow>(
    where: "score > @min",
    parameters: new { min = 5 },
    orderBy: "score DESC",
    limit: 10);

await db.UpdateAsync(new PlayerRow { Id = 1, Name = "A", Score = 99 });
await db.DeleteByKeyAsync<PlayerRow>(1);
```

动态表：

```csharp
using ReluctersteSDK.Sqlite.RowMod;

await using var ctx = new RowDbContext(db);
RowTable users = ctx["users"];
await users.CreateTableIfNotExistsAsync(("id", "INTEGER PRIMARY KEY"), ("name", "TEXT"));
await users.AddAsync(new Row { ["id"] = 1, ["name"] = "Alice" });
List<Row> all = await users.ToListAsync();
```

迁移：

```csharp
using ReluctersteSDK.Sqlite.Migrations;

await db.Migrations.MigrateAsync(new[]
{
    SqliteMigration.Create(1, "初始化",  async (c, ct) => await c.CreateTableIfNotExistsAsync<PlayerRow>(ct))
});
```
