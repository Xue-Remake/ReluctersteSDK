# ReluctersteSDK.EventSystem.Test

`ReluctersteSDK.EventSystem` 的 xUnit 单元测试工程。

- 框架：xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` 17.14.1
- 用例数：**90**

## 运行

```powershell
# 整个解决方案
dotnet test ReluctersteSDK.slnx -p:GeneratePackageOnBuild=false

# 只跑这个工程
dotnet test ReluctersteSDK.EventSystem.Test -p:GeneratePackageOnBuild=false
```

## 测试文件分布

| 文件 | 覆盖内容 |
|---|---|
| `EventBaseTests.cs` | `EventId` / `Timestamp` 稳定、UTC、`init` 固定值 |
| `EventBusCoreTests.cs` | 精确订阅、同步/异步重载、token 退订、重复订阅、`Unsubscribe`/`Clear`、重入、发布期间订阅/退订 |
| `EventBusDispatchTests.cs` | 精确/基类/多级基类/接口开关、顺序、Sequential/Parallel、struct 事件、死订阅抛异常 |
| `EventBusErrorTests.cs` | 聚合异常、`ThrowOnError`、`OnError`、同步异常、null Task、DI 解析失败、计数 |
| `EventBusCancellationTests.cs` | 预取消、中途取消、OCE 分类、顺序停止、并行等待在飞任务、取消+错误 |
| `EventBusTimeoutTests.cs` | 忽略 token、协作取消、自身 TimeoutException、顺序停止、并行隔离、Scope 释放时机 |
| `EventBusDiTests.cs` | Scoped/Singleton/Transient、接口注册、Activator 回退、构造失败、`IDisposable`/`IAsyncDisposable` 释放 |
| `EventHandlerAttributeTests.cs` | 公开/私有方法、void/Task/ValueTask、非法签名矩阵、继承/重载、Order、CancellationToken、部分注册 |
| `EventBusConcurrencyTests.cs` | 并发订阅/退订/发布/Clear、重入、无丢失订阅 |

## 测试约定

1. **超时测试留余量**：并行隔离用例用 250ms 超时，避免 CI 负载导致快处理器也被判超时。
2. **并发用例不依赖 `Thread.Sleep`**：用 `TaskCompletionSource(RunContinuationsAsynchronously)` 与 `Task.WaitAsync` 控制时序。
3. **静态计数器只在单个测试类内使用**：xUnit 默认并行运行不同测试类，跨类共享静态状态会互相污染。
4. **DI 用例自己创建 `ServiceProvider`**：`using` 释放，避免 Scope 泄漏。
5. **超时用例会留下后台处理器**：处理器故意不响应取消，测试结束时不等待它；这是为了验证“超时只是不再等待”。

## 当前解决方案的测试分布

| 工程 | 用例数 |
|---|---|
| `ReluctersteSDK.Paths.Test` | 41 |
| `ReluctersteSDK.Math.Test` | 83 |
| `ReluctersteSDK.Game.Test` | 119 |
| `ReluctersteSDK.DataStructures.Test` | 11 |
| `ReluctersteSDK.EventSystem.Test` | 90 |
| `ReluctersteSDK.Sqlite.Test` | 17 |
| 合计 | **361** |
