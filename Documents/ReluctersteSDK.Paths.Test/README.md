# ReluctersteSDK.Paths.Test

`ReluctersteSDK.Paths` 的 xUnit 单元测试工程。

- 框架：xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` 17.14.1
- 用例数：**41**（27 个 `[Fact]` + 4 个 `[Theory]` 展开为 14 条）

## 运行

```powershell
# 整个解决方案
dotnet test ReluctersteSDK.slnx -p:GeneratePackageOnBuild=false

# 只跑这个工程
dotnet test ReluctersteSDK.Paths.Test -p:GeneratePackageOnBuild=false
```

## 测试文件分布

| 文件 | 用例数 | 覆盖内容 |
|---|---|---|
| `FdPathTests.cs` | 7 | 绝对/相对路径构造、`*` 拼接、`/` 求相对路径、相等性 |
| `PathSegmentTests.cs` | 5 | `PathSegment.Create` 对盘符/UNC/`.`/`..`/扩展名的判定 |
| `PathValidatorTests.cs` | 7 + 14 | 合法性校验、格式化、保留名、绝对路径严格判定 |
| `PathAnalyzerTests.cs` | 4 | 相对 ↔ 绝对互转，基准为 `AppContext.BaseDirectory` |
| `PhysicalPathHelperTests.cs` | 4 | 存在性检查、目录创建、文件枚举 |

## 当前解决方案的测试分布

| 工程 | 用例数 | 覆盖 |
|---|---|---|
| `ReluctersteSDK.Paths.Test` | 41 | 路径解析、校验、运算、物理操作 |
| `ReluctersteSDK.Math.Test` | 83 | 向量、矩阵、几何、插值、数值、随机、噪声 |
| `ReluctersteSDK.Game.Test` | 119 | ECS 核心、层级、查询、调度、变换、渲染、摄像机、快照、统计 |
| `ReluctersteSDK.DataStructures.Test` | 11 | 拓扑节点 `NodeBase` |
| `ReluctersteSDK.EventSystem.Test` | 90 | 事件元数据、分发、错误、取消、超时、DI、特性扫描、并发 |
| `ReluctersteSDK.Sqlite.Test` | 17 | 迁移升级/幂等/失败回滚/乱序校验/回滚/DDL 助手 |
| 合计 | **361** | |

**没有测试的模块**：`Registry`、`Config`、`Console`、`Llm`。

> 这几个模块的并发语义（注册表启停、ORM 写锁）恰恰是最需要测试的地方，建议后续补。
> 顶层 `RegistryTests/` 工程**未加入解决方案**，且测的是旧原型（命名空间 `Registry`），与 `dotnet test ReluctersteSDK.slnx` 无关。

## 写测试时的注意事项

1. **基准目录是 `AppContext.BaseDirectory`**，测试运行时指向 `bin/Debug/net10.0/`，不是工程根目录。
   涉及物理文件的用例要么用 `Path.GetTempPath()`，要么把断言写成相对形式。

2. **路径相等是 `OrdinalIgnoreCase`**，Windows 下 `E:\Work` 与 `e:\work` 相等。跨平台跑测试时这条会挂。

3. 涉及真实文件的用例要**自己清理**（`try/finally` 或 `IDisposable`），xUnit 不会自动回收。

## 模板

```csharp
using ReluctersteSDK.Paths;

namespace ReluctersteSDK.Paths.Test;

public class MyNewTests
{
    [Fact]
    public void RelativePath_Should_Prefix_With_Dot()
    {
        var path = new RelativePath(@"Data\Databases");

        Assert.Equal(@".\Data\Databases", path.PathStr);
    }

    [Theory]
    [InlineData(@"C:\", true)]
    [InlineData("C:", false)]
    public void IsValidAbsolutePath_Should_Match(string input, bool expected)
    {
        Assert.Equal(expected, PathValidator.IsValidAbsolutePath(input));
    }
}
```
