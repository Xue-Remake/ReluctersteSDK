# ReluctersteSDK.Sqlite

基于 `Microsoft.Data.Sqlite` 的**极简异步 ORM**。它同时提供两套互不干扰的用法，并把数据库结构演进收进一个版本化迁移器：

| 模式 | 入口 | 适合 |
|---|---|---|
| **特性映射** | `SqliteDataBase` + `[DbTable]` / `[DbKey]` / `[DbColumn]` | 有固定实体类、强类型 CRUD |
| **纯行模式（RowMod）** | `RowDbContext` / `RowTable` / `RowQuery` | 动态表、脚本、schema 未定 |
| **迁移** | `db.Migrations` | 已上线数据库的加列、建索引、回滚 |

- 目录：`ReluctersteSDK.Sqlite/`
- 包 ID：`ReluctersteSDK.Sqlite`
- 命名空间：`ReluctersteSDK.Sqlite` / `.RowMod` / `.Migrations`
- 依赖：`ReluctersteSDK.Paths`、`Microsoft.Data.Sqlite`
- 目标框架：`net10.0`

---

## 它解决什么问题

不是"另一个 EF Core"。它瞄准的是**小型本地库**：游戏存档、配置、工具数据、原型验证。

| 你原本要写的东西 | 本包的处理 |
|---|---|
| 连接管理 | 一个 `SqliteDataBase` 代表一个库文件；每次操作自动开连接并释放 |
| PRAGMA 初始化 | `EnsureInitializedAsync`：WAL、`busy_timeout`、外键、`synchronous` |
| 实体 ↔ 表映射 | 四个特性 + 每种类型只反射一次并缓存 |
| SQL 拼接与参数绑定 | 值和主键自动参数化；DDL 标识符自动加引号转义 |
| 动态 schema | RowMod：`Row` 是大小写不敏感的列字典 |
| 结构升级 | `SqliteMigrationRunner`：版本化、事务化、可回滚、有历史表 |
| 并发写冲突 | 全局写锁串行化；读走 WAL |

---

## 设计原则

### 1. 两套模式，各自完整

特性映射模式面对**编译期已知的实体**；RowMod 面对**运行期才知道的表**。两者共用同一个 `SqliteDataBase`，但 API 不互相依赖，可以同时使用。

### 2. 显式优于隐式

- 只有标了 `[DbKey]` / `[DbColumn]` 的属性才入库，其余默认忽略；
- 查询条件 `where` / `orderBy` 是显式 SQL 片段，值必须走参数；
- 迁移必须手写版本与步骤，**不做自动结构比对**。

这是刻意的：自动推断在小型项目里省一点事，在出问题时会让"到底改了什么"变得不可追溯。

### 3. 写操作串行，读操作并发

所有写路径经过 `SemaphoreSlim(1,1)`；读路径不加锁，依赖 WAL + `busy_timeout=5000`。迁移也持有同一把写锁，因此迁移期间普通写操作会排队。

### 4. 类型映射保守

只支持 BCL 基础类型、枚举、`DateTime` / `DateTimeOffset`、`Guid`、`byte[]` 与它们的 `Nullable<T>`。自定义类 / 结构体在**建表阶段**直接抛 `NotSupportedException`，不给你"运行时才发现存不进去"的机会。

### 5. 失败要能定位

- CRUD 返回受影响行数，不吞异常；
- 迁移失败抛 `SqliteMigrationException`，带版本、说明、方向与原始异常；
- 未注册的实体 / 缺主键 / 键数量不匹配都有明确异常。

---

## 模块结构

```
ReluctersteSDK.Sqlite/
├─ SqliteDataBase.cs              连接、初始化、CRUD、原生 SQL
├─ DbAttributes.cs                [DbTable] / [DbKey] / [DbColumn] / [DbIgnore]
├─ MetaEntity.cs                  EntityMap / ColumnMap / EntityMapCache
├─ SqliteMapping.cs               SqliteTypeMapper（.NET ↔ SQLite）
├─ SqliteSchemaSql.cs             按映射生成 CREATE TABLE
├─ SqlTool.cs                     标识符引用、参数装配
├─ Migrations/
│  ├─ SqliteMigration.cs          迁移基类 + 委托工厂
│  ├─ SqliteMigrationRunner.cs    执行、历史表、回滚、乱序校验
│  ├─ SqliteMigrationContext.cs   事务内执行 + 结构探测 + DDL 助手
│  └─ MigrationModels.cs          MigrationResult / AppliedMigration
└─ RowMod/
   ├─ Row.cs                      大小写不敏感的列字典
   ├─ RowDbContext.cs             表访问入口与释放
   ├─ RowTable.cs                 单表增删查
   └─ RowQuery.cs                 链式查询构建器
```

---

## 能力总览

### 特性映射

| 能力 | 入口 |
|---|---|
| 建表 | `EnsureTableAsync<T>()` |
| 插入 / 插入或更新 | `InsertAsync` / `UpsertAsync` |
| 更新 / 删除 | `UpdateAsync` / `DeleteAsync` / `DeleteByKeyAsync` |
| 按主键查询 | `GetByKeyAsync<T>(params object?[] keys)` |
| 条件查询 | `QueryAsync<T>(where, parameters, orderBy, limit)` |
| 全表读取 | `GetAllAsync<T>()` |

### 原生 SQL

| 能力 | 入口 |
|---|---|
| 非查询 | `ExecuteAsync(sql, parameters)` |
| 标量 | `ExecuteScalarAsync(sql, parameters)` |
| 自定义投影 | `QuerySqlAsync<TResult>(sql, reader => …, parameters)` |

### 纯行模式

| 能力 | 入口 |
|---|---|
| 表访问 | `RowDbContext.Table(name)` / `ctx["name"]` |
| 建表 | `RowTable.CreateTableIfNotExistsAsync((列名, 列定义)…)` |
| 插入 | `AddAsync`（单行）/ `AddRangeAsync`（单事务） |
| 查询 | `Where(lambda)` / `Select(columns)` / `OrderBy…` / `Skip` / `Take` |
| 批量修改 | `RowQuery.UpdateAsync(Row)` / `DeleteAsync()` |

### 迁移

| 能力 | 入口 |
|---|---|
| 应用迁移 | `db.Migrations.MigrateAsync(migrations)` |
| 回滚 | `db.Migrations.RollbackAsync(migrations, targetVersion)` |
| 历史 | `db.Migrations.GetAppliedMigrationsAsync()` |
| DDL 助手 | `CreateTableIfNotExistsAsync<T>` / `AddColumnAsync` / `CreateIndexAsync` 等 |
| 结构探测 | `TableExistsAsync` / `ColumnExistsAsync` / `GetColumnsAsync` |

---

## 典型使用路径

```
特性映射：建库 → EnsureInitializedAsync → EnsureTableAsync<T> → Insert/Query/Update/Delete
RowMod：  建库 → RowDbContext → CreateTableIfNotExistsAsync → AddRange → Where(...).ToListAsync
迁移：    建库 → 定义 SqliteMigration[] → MigrateAsync → 后续版本追加，启动时幂等执行
```

**第一次使用请从 [Tutorial/01-快速开始.md](Tutorial/01-快速开始.md) 开始**——那里有一个完整的控制台程序。

---

## 文档导航

| 文档 | 内容 |
|---|---|
| **[Tutorial/](Tutorial/README.md)** | 从建库到迁移、并发与排错的完整示例 |
| [API/SqliteDataBase.md](API/SqliteDataBase.md) | 构造、初始化、原生 SQL、类型映射、并发模型 |
| [API/AttributeMapping.md](API/AttributeMapping.md) | 四个特性、映射规则、CRUD 方法细节 |
| [API/RowMod.md](API/RowMod.md) | `RowDbContext` / `RowTable` / `RowQuery` / `Row` 与表达式支持范围 |
| [API/Migrations.md](API/Migrations.md) | 版本化迁移、历史表、DDL 助手、异常与限制 |

---

## 设计取舍

| 决策 | 选择 | 理由 |
|---|---|---|
| 连接模型 | 每次操作新建连接 | 小型本地库不需要连接池；避免长连接跨线程与 WAL 生命周期问题 |
| 写并发 | 全局 `SemaphoreSlim(1,1)` | SQLite 单写者模型下最简单可靠；批量场景用 `AddRangeAsync` 或迁移 |
| 读并发 | 不加锁 | WAL 允许读写并行；`busy_timeout` 兜住偶发冲突 |
| 映射 | 特性 + 反射缓存 | 无源生成器依赖；每种类型只反射一次 |
| 查询条件 | SQL 片段 + 参数 | 表达力完整；代价是片段本身不校验、不转义 |
| RowMod 查询 | 表达式树翻译 | 覆盖常见比较/逻辑/`Contains`；不支持的子表达式编译后当参数 |
| 迁移 | 手写版本，不做 diff | 变更可审计；避免自动推断改错表 |
| 事务 | 只在 `AddRangeAsync` 与迁移内部使用 | 对外暴露事务会把连接/锁的生命周期泄漏给调用方 |

---

## 已知限制

- **特性映射没有公开事务 API**：跨多次 `Insert` / `Update` 不保证原子性；需要事务请写原生 SQL 或走迁移上下文。
- **`where` / `orderBy` / 列类型是裸 SQL 片段**：只接受代码里写死的字符串，**不要拼用户输入**；值一律走 `parameters`。
- **`QueryAsync` 依赖 SELECT 别名等于属性名**：手工改 SQL 时注意别名。
- **`UpdateAsync` 覆盖全部非键列**：只改一列请用 `ExecuteAsync` 或 RowMod 的 `UpdateAsync(Row)`。
- **建表不迁移**：`EnsureTableAsync` 只做 `CREATE TABLE IF NOT EXISTS`；结构变更用 `db.Migrations`。
- **不支持复杂类型 / 继承分层**：`List<>`、自定义类会在建表阶段失败。
- **RowMod 表达式不校验列名**：拼错要到执行期才报错；不支持 `JOIN` / 子查询 / 分组。
- **`RowQuery` 是可变构建器**：重复执行同一实例会累积条件；`FirstOrDefaultAsync()` 会把 `LIMIT` 永久改为 1。
- **`RowDbContext.DisposeAsync` 连带释放底层库**：不要对 `db` 和 `ctx` 同时 `await using`。
- **无重试**：`SQLITE_BUSY` 只等 `busy_timeout`，超时后抛异常。

---

## 构建与测试

```powershell
dotnet build ReluctersteSDK.Sqlite/ReluctersteSDK.Sqlite.csproj -p:GeneratePackageOnBuild=false
dotnet test ReluctersteSDK.Sqlite.Test/ReluctersteSDK.Sqlite.Test.csproj -p:GeneratePackageOnBuild=false
```

当前基线：**构建 0 错误**，`ReluctersteSDK.Sqlite.Test` **17 个用例全部通过**（迁移执行/幂等/回滚/乱序校验/DDL 助手）。
