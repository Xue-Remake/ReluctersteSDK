# CommandHelper（示例工程）

基于 `ReluctersteSDK.Registry` 实现的**高级命令处理器**，是 `ReluctersteSDK.Commands` 的进阶版。

- 类型：`Exe` 示例工程，**不打包成 NuGet**
- 命名空间：`CommandHelper`
- 依赖：`ReluctersteSDK`（聚合包，实际只用到 `ReluctersteSDK.Registry`）

> 需要"轻量、无依赖"就用 [CommandProcessor](../ReluctersteSDK.Commands/README.md)；
> 需要特性扫描和 DI 才用这里。

## 相比 `CommandProcessor` 多了什么

| 能力 | `CommandProcessor` | `AdvancedCommandProcessor` |
|---|---|---|
| 注册命令 | ✅ | ✅ |
| 别名 | ✅ | ✅ |
| 启用 / 禁用 | ✅ | ✅（基于 `ValueRegistry`，并发安全） |
| 特性扫描自动注册 | ❌ | ✅ `[Command]` |
| 命令内取服务 | ❌ | ✅ `context.Services` |
| 大小写敏感 | 不敏感（`OrdinalIgnoreCase` 字典） | 不敏感（注册时 `ToLower()`） |

---

## 场景一：最小可运行

```csharp
using CommandHelper;
using ReluctersteSDK.Registry;

public class DataService
{
    public string GetData() => "核心数据加载成功";
}

public class SystemCommands
{
    [Command("sysinfo", "查看系统信息", "info", "sys")]
    public void ShowSysInfo(CommandContext context)
    {
        Console.WriteLine("系统状态：运行中");

        if (context.Services.TryResolve<DataService>(out var data))
            Console.WriteLine($"服务状态：{data.GetData()}");
    }
}

class Program
{
    static async Task Main()
    {
        var services = new InstanceRegistry();
        services.RegisterInstance(new DataService());

        var cli = new AdvancedCommandProcessor(services);
        cli.RegisterFromTarget(new SystemCommands());
        cli.RegisterHelpCommand();

        await cli.ExecuteAsync("sys");       // 走别名
    }
}
```

---

## 场景二：手动注册（不用特性）

```csharp
cli.Register("load", "加载存档", async ctx =>
{
    Console.WriteLine($"加载 {ctx.Args.FirstOrDefault() ?? "default.sav"}");
    await Task.CompletedTask;
}, "l", "open");

cli.Register("quit", "退出", _ => Environment.Exit(0), "q");
```

---

## 场景三：命令方法的签名要求

`[Command]` 标记的方法**必须且只能有一个 `CommandContext` 参数**，返回 `void` 或 `Task`：

```csharp
[Command("echo", "回显参数")]
public void Echo(CommandContext ctx) => Console.WriteLine(string.Join(" ", ctx.Args));

[Command("sleep", "异步等待")]
public async Task Sleep(CommandContext ctx) => await Task.Delay(1000);
```

签名不符会在 `RegisterFromTarget` 时抛 `InvalidOperationException`。

`RegisterFromTarget` 扫描的是 `Public | NonPublic | Instance` 方法，所以 `private` 方法标了 `[Command]` 也会被注册。

---

## 场景四：命令里取依赖

```csharp
public record CommandContext(string RawInput, string[] Args, InstanceRegistry Services);
```

比 `CommandProcessor` 的方案多一个 `Services`。命令内部可自由解析依赖，不需要把服务塞进构造函数：

```csharp
[Command("save")]
public void Save(CommandContext ctx)
{
    if (ctx.Services.TryResolve<SaveSystem>(out var save))
        save.WriteSlot(1);
    else
        Console.WriteLine("SaveSystem 未注册");
}
```

---

## 场景五：启停命令

```csharp
cli.Disable("sysinfo");     // 联动禁用别名 info / sys
await cli.ExecuteAsync("info");   // → "[提示] 指令 'info' 当前已被禁用。"

cli.Enable("sysinfo");
cli.Unregister("sysinfo");  // 主键 + 别名一起删除
```

---

## ⚠️ 已知问题（用它上生产前建议先修）

### 1. `Unregister` 无法注销已禁用的命令

```csharp
cli.Disable("quit");
cli.Unregister("quit");   // ❌ 返回 false，什么也没删
```

原因：`Unregister` 用 `_commandRegistry.TryGetValue(key, out ...)` 反查命令定义，而 `TryGetValue` **会过滤掉禁用条目**，于是拿不到 `command`，主键和别名都删不掉。

绕过办法是先 `Enable` 再 `Unregister`。

**根治**：维护一张 `别名 → 主键` 的独立映射表，不依赖注册表的过滤语义。

### 2. `Enable` 里有一次无用的查询

```csharp
if (_commandRegistry.TryGetValue(cmd => 匹配 trigger 或别名, out var commands) || _commandRegistry.Exists(key))
```

`commands` 从未被使用，真正起作用的是后半段的 `|| Exists(key)`。功能正确，但有冗余遍历。

### 3. `help` 列表看不到已禁用的命令

`ValueRegistry.TryGetValue(_ => true, ...)` 只返回**已启用**的条目，所以 `help` 里禁用的命令直接消失，而不是像 `CommandProcessor` 那样标注 `[已禁用]`（深灰显示）。

### 4. 词法解析仍是空格切分

不支持引号与转义。`set "hello world"` 会被切成 `set` / `"hello` / `world"`。

### 5. `Services` 是 `InstanceRegistry` 而非真正的 DI 容器

不支持构造函数注入、生命周期管理、Scoped。`RegisterFromTarget` 只能扫描**已有实例**上的方法，没法让容器替你造对象。

---

## 运行

```powershell
dotnet run --project CommandHelper/CommandHelper.csproj
```
