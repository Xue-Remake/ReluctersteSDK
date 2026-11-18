# Changelog

## 0.13.0.0（2026-11-16）

### Added

- Game：`EventQueue<T>.ReadList` 与 `WorldEvents.ReadList<T>()` —— 零分配的 `IReadOnlyList<T>` 只读视图。
  与 `Read` 的 span 互补：可以安全地存进局部变量或传给其他方法，但不能用于 `ref readonly` 遍历。
  注意读缓冲由两个列表交替复用，下一次 `Swap` 后旧引用会被清空。
- Game.Test 从 131 个用例扩展到 135 个。

## 0.12.0.0（2026-11-14）

### Added

- Game：双缓冲后置消费事件系统。
  - `WorldEvents`（`World.Events`）：按类型组织的双缓冲事件容器。
  - `EventQueue<T>`：`Raise` 写缓冲、`Read` 返回读缓冲的 `ReadOnlySpan<T>`、`Swap` 交换，零分配。
  - `World.Update` 在开头自动交换缓冲：第 N 帧登记的事件在第 N+1 帧可读；Startup 阶段登记的事件首帧即可读。
  - 遍历读缓冲时继续 `Raise` 是安全的（读写是两个独立列表）。
  - `WorldStats` 新增 `EventQueueCount` / `PendingEventCount`。
  - `World.Clear()` / `Dispose()` 一并清空事件。
- Game.Test 从 119 个用例扩展到 131 个。

### 说明

- 该子系统**不依赖** `ReluctersteSDK.EventSystem`，也不提供异步、委托订阅或类型继承分发。
  跨模块广播请在应用层桥接到外部事件总线。

## 0.11.0.0（2026-11-13）

### Added

- Config：`IConfigCodec<T>` 与 `JsonConfigCodec<T>`。后者可从 `JsonSerializerContext` 取源生成元数据
  （`JsonConfigCodec<T>.FromContext`），读写与主键全部显式化，运行时不反射类型成员。
- Config：`JsonFileConfigStore<T>` 新增接受 `IConfigCodec<T>` 的构造重载。
- Config：`ConfigRegistry` —— 配置节自注册、按类型取用、保存成功后触发 `Changed` 事件。
- Config：`AddReluctersteSDKConfig(this IServiceCollection, ...)` 依赖注入扩展，只依赖
  `Microsoft.Extensions.DependencyInjection.Abstractions`。
- Config.Test 从 9 个用例扩展到 17 个。

### Changed

- 全部库与聚合包版本升级到 `0.11.0.0`。

## 0.10.0.0（2026-11-12 ~ 2026-11-13）

> 2026-11-13 追加了包名统一、注册表修复与 Config 重做，并入同一版本。

### Breaking

- 子包名统一为"无后缀领域名词"，目录名 = 工程名 = 包 ID = 程序集名 = 根命名空间：
  `ConsoleHelper` → **`Commands`**、`LlmHelper` → **`Llm`**、`PathHelper` → **`Paths`**、
  `RegistryKit` → **`Registry`**、`ConfigDriver` → **`Config`**、`SimpleSqliteORM` → **`Sqlite`**。
  不保留旧的包名与命名空间。
- `ReluctersteSDK.Config` 的四个静态 Manager（`ConfigManager` / `JsonConfigManager` / `ItemManager` / `InventoryManager`）
  与 `IJsonConfig` 全部删除，改为 `IConfigStore<T>` 与 `EntityConfigStore<TEntity, TData>`。详见模块 README 的迁移表。

- `ReluctersteSDK.MathTool` 改名为 **`ReluctersteSDK.Math`**：目录、工程、包 ID、命名空间全部变更，不保留兼容层。
- `ReluctersteSDK.Monogame/` 目录改名为 **`ReluctersteSDK.Game/`**，与包 ID 一致。
- `ReluctersteSDK.Math` 不再引用 MonoGame；`CartesianVector2D` 与 `Vector2` 的隐式转换移除，改用 Game 包的 `ToXna` / `ToCartesian` 扩展。
- `CartesianDoubleVector2D.ToVector2()` 改名为 `ToCartesianVector2D()`。
- `World.Get<T>` 对失效实体抛 `ArgumentException`；`TryGet/Has/Remove` 返回 `false`。
- `World.Add<T>` 覆盖已有组件不再自增 `StructuralVersion`，也不触发 `ComponentAdded`。
- `Children.Items` 改为只读视图。
- `Animator.Speed` 忽略非正数、NaN 与 Infinity。
- 删除半成品 `ConsoleGame`。

### Added

- Math：`Vector2Int`、`Matrix3x2Extensions`、`Matrix3x2D`、`Transform2DMath`、2D 几何体、插值/缓动、数值工具、`SeededRandom`（PCG32）/`IRandomSource`/`DistributionSampler`、Perlin/Value 噪声。
- Game：3/4 组件查询与 `Without`、系统阶段/优先级/启停与 `OnStart/OnStop`、单例组件、生命周期事件、`World.Dispose`、命令缓冲增强、Transform 脏标记与拓扑缓存、`Camera2D` 与裁剪、`WorldSnapshot` 序列化、`WorldStats` 调试统计。
- 新增 `ReluctersteSDK.Math.Test`（83 个测试）；Game.Test 扩展到 119 个。
- Config：`IConfigStore<T>`、`ConfigQuery<T>`、`ConfigSaveResult`、`InMemoryConfigStore<T>`、
  `JsonFileConfigStore<T>`（原子写入）、`SqliteConfigStore<T>`（自动建表）、`EntityConfigStore<TEntity, TData>`。
- 新增 `ReluctersteSDK.Registry.Test`（12 个）与 `ReluctersteSDK.Config.Test`（9 个）。

### Changed

- 所有库工程与聚合包版本统一为 `0.10.0.0`。
- Game 包移除未使用的 `ReluctersteSDK.DataStructures` 依赖。

### Fixed

- `TransformSystem`：父节点有 Transform、子节点没有时不再抛 `KeyNotFoundException`。
- `TransformSystem`：空闲帧不再持续自增 `StructuralVersion`。
- `Animator`：`Speed = Infinity` 导致的潜在死循环。
- `World.Store<T>()` 直接增删组件现在也会维护结构版本与生命周期事件。
- `Registry`：`CompositeKey.Keys` 改为真正的只读视图，堵住通过强转数组破坏哈希的不变量；
  `default(CompositeKey)` 不再抛 `NullReferenceException`。
- `Registry`：`InstanceRegistry.TryResolve` 在工厂抛异常时返回 `false`，与 `TypeRegistry.TryCreateInstance` 一致。
- `Registry`：`RegistryJson.Default` 补 `volatile`，修正双重检查锁的内存可见性问题。
- `Config`：JSON 写入改为临时文件 + 覆盖移动，不再产生截断文件；所有异步 API 接受 `CancellationToken`。
