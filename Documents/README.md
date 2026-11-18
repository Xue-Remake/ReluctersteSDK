# ReluctersteSDK 使用文档

本文档目录与 `ReluctersteSDK.slnx` 平级，按模块分文件夹整理。

- 目标框架：**net10.0**（全部模块）
- 当前版本：**0.13.0.0**
- 打包输出：`E:\Work\资产仓库\NuGetPackage`（各库工程 `GeneratePackageOnBuild=True`，`dotnet build` 即自动出包）

## 文档约定

每个**库模块**文件夹固定两个部分；已成体系、示例较多的模块（`Game`、`Sqlite`）增加第三个部分 `Tutorial/`：

| 文件 | 写什么 |
|---|---|
| `README.md` | **总体介绍**：是什么、解决什么问题、设计原则与取舍、能力总览、文档导航、已知限制。不写长示例。 |
| `Tutorial/*.md` | **完整用法示例**：按主题分章，每章一个可运行/可抄用的场景，含完整代码与陷阱。仅 `Game`、`Sqlite` 提供。 |
| `API/*.md` | **参考手册**：按主题拆分的成员表、参数语义、异常、边界与已知限制。 |

对没有 `Tutorial/` 的模块，`README.md` 仍以场景用例为主（`我想做 X` → 代码 → 要点），
因为它们的 API 面较小，一个文件就能覆盖“怎么开始 + 怎么用”。

这样查"这个包是什么"看 README，查"怎么一步步用"看 Tutorial，查"这个方法到底返回什么"看 API。

示例工程（`CommandHelper/`、`ConsoleApp1/`）与测试工程只有 README，没有 `API/`——它们的"参考"就是源码本身。
聚合包 `ReluctersteSDK/` 也只保留 README：它没有自己的 API，内容全是打包与版本注意事项。

```
ReluctersteSDK.Registry/            ReluctersteSDK.Game/
├─ README.md                        ├─ README.md          ← 总体介绍
└─ API/                             ├─ Tutorial/          ← 完整用法示例（01~12）
   ├─ ValueRegistries.md            └─ API/
   ├─ FactoryRegistries.md             ├─ Entity.md
   └─ Json.md                          └─ …
```

> [!NOTE]
> 目录名、工程文件名、包 ID、程序集名、根命名空间**现在完全一致**：
> 目录 `ReluctersteSDK.Xxx/` 对应包 `ReluctersteSDK.Xxx`。
> 所有库工程都显式写了 `PackageId` / `AssemblyName` / `RootNamespace`，重命名目录不会再悄悄改变包标识。

## 模块索引

| 模块 | 包 ID | 一句话说明 |
|---|---|---|
| [ReluctersteSDK](ReluctersteSDK/README.md) | `ReluctersteSDK` | 聚合包，引用下列全部子模块 |
| [ReluctersteSDK.Registry](ReluctersteSDK.Registry/README.md) | 同名 | 线程安全注册表：键值、复合键、类型、实例、委托 |
| [ReluctersteSDK.Paths](ReluctersteSDK.Paths/README.md) | 同名 | 路径抽象（绝对/相对）、合法性校验、物理文件操作 |
| [ReluctersteSDK.DataStructures](ReluctersteSDK.DataStructures/README.md) | 同名 | 双向字典、线程安全列表字典、图结构、拓扑节点 |
| [ReluctersteSDK.Math](ReluctersteSDK.Math/README.md) | 同名 | 零 MonoGame 依赖的 2D 数学库：向量、矩阵、几何、插值/缓动、数值工具、可种子随机、噪声 |
| [ReluctersteSDK.Game](ReluctersteSDK.Game/README.md) | 同名 | MonoGame 2D ECS：实体/组件/系统、层级变换、查询与调度、渲染、摄像机、快照、双缓冲事件 |
| [ReluctersteSDK.EventSystem](ReluctersteSDK.EventSystem/README.md) | 同名 | 异步事件总线：基类分发、令牌退订、错误聚合、取消/超时、特性扫描 |
| [ReluctersteSDK.Sqlite](ReluctersteSDK.Sqlite/README.md) | 同名 | SQLite ORM：特性映射模式 + 纯行模式 + 版本化迁移 |
| [ReluctersteSDK.Config](ReluctersteSDK.Config/README.md) | 同名 | 配置存储抽象：内存 / JSON / SQLite 三后端、显式 codec、配置节注册表 |
| [ReluctersteSDK.Commands](ReluctersteSDK.Commands/README.md) | 同名 | 控制台命令处理器 |
| [ReluctersteSDK.Llm](ReluctersteSDK.Llm/README.md) | 同名 | OpenAI 兼容的 LLM 客户端 + 配置管理 |

> **Tutorial**：`Game` 与 `Sqlite` 已改为"总体介绍 README + 分章 Tutorial + API 参考"三段式，
> 入口分别是 [Game/Tutorial](ReluctersteSDK.Game/Tutorial/README.md) 与
> [Sqlite/Tutorial](ReluctersteSDK.Sqlite/Tutorial/README.md)。其余模块仍以 README 的场景用例为主。

### 示例与测试工程

| 目录 | 类型 | 说明 |
|---|---|---|
| [CommandHelper](CommandHelper/README.md) | 示例 | 基于 Registry 的高级命令处理器（特性扫描 + DI） |
| [ConsoleApp1](ConsoleApp1/README.md) | 示例 | Sqlite 纯行模式（RowMod）完整用例 |
| [ReluctersteSDK.Paths.Test](ReluctersteSDK.Paths.Test/README.md) | 测试 | Paths 的 xUnit 用例（41 个） |
| `ReluctersteSDK.Math.Test/` | 测试 | 向量、矩阵、几何、插值、数值、随机、噪声（83 个） |
| `ReluctersteSDK.Game.Test/` | 测试 | ECS 核心、层级变换、查询、调度、渲染、摄像机、快照、统计、双缓冲事件（135 个） |
| `ReluctersteSDK.DataStructures.Test/` | 测试 | 拓扑节点 `NodeBase`（11 个） |
| `ReluctersteSDK.EventSystem.Test/` | 测试 | 事件元数据、分发、错误、取消、超时、DI、特性扫描、并发（90 个） |
| `ReluctersteSDK.Sqlite.Test/` | 测试 | 迁移执行/幂等/失败回滚/乱序校验/回滚/DDL 助手（17 个） |
| `ReluctersteSDK.Registry.Test/` | 测试 | 复合键相等性/只读视图、五个注册表、JSON 往返（12 个） |
| `ReluctersteSDK.Config.Test/` | 测试 | 三种存储、实体适配器、取消、源生成 codec、配置节注册表与 DI（17 个） |

### 设计与其他

| 文档 | 说明 |
|---|---|
| [ECS 设计](ReluctersteSDK.Game/ECS-设计.md) | Game 模块的 ECS 设计稿：分层原则、取舍、落地顺序 |
| [待修问题清单](待修问题清单.md) | 文档整理时静态审阅出的 36 项问题，按 P0~P3 分级 |

## 依赖关系

```
ReluctersteSDK（聚合）
├─ ReluctersteSDK.Registry          ← 无依赖
├─ ReluctersteSDK.Paths           ← 无依赖
├─ ReluctersteSDK.DataStructures       ← 无依赖
├─ ReluctersteSDK.Math                 ← 无依赖（零 MonoGame）
├─ ReluctersteSDK.EventSystem          ← Microsoft.Extensions.DependencyInjection
├─ ReluctersteSDK.Commands             ← 无依赖
├─ ReluctersteSDK.Llm                  ← 无依赖
├─ ReluctersteSDK.Game                 ← Math + MonoGame.Framework.DesktopGL
├─ ReluctersteSDK.Sqlite               ← Paths + Microsoft.Data.Sqlite
└─ ReluctersteSDK.Config               ← Paths + Sqlite + DI Abstractions
```

## 未纳入文档的工程

| 工程 | 状态 |
|---|---|
| `ReluctersteSDK.Debugger` | 按计划**完全重构**中，短期不在生产项目使用，暂不提供文档 |

旧原型与死工程（`DynamicORM/`、顶层 `RegistryKit/`、`RegistryKitTests/`、`TestProject/`）已在 0.13.0.0 中删除。

## 构建与验证

```powershell
# 构建（禁用打包，避免写 NuGet 输出目录）
dotnet build ReluctersteSDK.slnx -p:GeneratePackageOnBuild=false

# 测试
dotnet test ReluctersteSDK.slnx -p:GeneratePackageOnBuild=false
```

当前基线（2026-11-16 全量验证）：**构建 0 错误 / 67 警告**（几乎全是 `CS1591` 与可空性警告；Math/Game/Registry/Config 为 0 警告），**测试 406/406 通过**（Math 83 + Game 135 + Paths 41 + DataStructures 11 + EventSystem 90 + Sqlite 17 + Registry 12 + Config 17）。
