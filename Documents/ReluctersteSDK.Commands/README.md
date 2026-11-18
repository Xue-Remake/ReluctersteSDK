# ReluctersteSDK.Commands（包 ID：`ReluctersteSDK.Commands`）

控制台命令解析与分发的极简实现：注册命令 → 读一行输入 → 按空格分词 → 执行处理器。

- 目录：`ReluctersteSDK.Commands/`
- 包 ID：`ReluctersteSDK.Commands`
- 命名空间：`ReluctersteSDK.Commands`
- 依赖：无

> 需要**特性扫描自动注册 + DI** 请用示例工程 [CommandHelper](../CommandHelper/README.md) 里的 `AdvancedCommandProcessor`。
> 本模块适合"轻量、无依赖"的场景。

---

## 场景一：一个能跑的命令行

**目标**：`load` / `quit` 两条命令 + 内置帮助。

```csharp
using ReluctersteSDK.Commands;

var cli = new CommandProcessor();

// 异步命令，带别名 l / open
cli.Register("load", "加载存档", async ctx =>
{
    string path = ctx.Args.Length > 0 ? ctx.Args[0] : "default.sav";
    Console.WriteLine($"加载 {path}");
    await Task.CompletedTask;
}, "l", "open");

// 同步命令，带别名 q / exit
cli.Register("quit", "退出", _ => Environment.Exit(0), "q", "exit");

cli.RegisterHelpCommand();      // 内置 help / ? / h

while (true)
{
    Console.Write("> ");
    await cli.ExecuteAsync(Console.ReadLine());
}
```

`Register` 返回 `this`，所以也可以链式写。

---

## 场景二：自定义未知命令的提示

```csharp
cli.OnUnknownCommand = name =>
{
    Console.WriteLine($"没有这个命令：{name}，输入 help 查看列表");
    return Task.CompletedTask;
};
```

不设置时会用黄字打印 `[Warning] 未知指令: 'xxx'`。

---

## 场景三：运行时开关某条命令

**目标**：调试期间临时禁掉 `quit`。

```csharp
cli.Disable("q");        // 主键或别名都行，会联动影响这条命令的所有别名
cli.Enable("quit");
cli.Unregister("quit");  // 主键 + 所有别名一起删除
```

被禁用的命令执行时打印提示，不会走 `OnUnknownCommand`。

---

## 场景四：读参数

```csharp
cli.Register("set", "设置", ctx =>
{
    if (ctx.Args.Length < 2) { Console.WriteLine("用法: set <key> <value>"); return; }
    Console.WriteLine($"设置 {ctx.Args[0]} = {ctx.Args[1]}");
});
```

`CommandContext.Args` 是**按空格切分**后的参数（第一个触发词已被切掉）。

> 不支持引号包裹、不支持带空格的参数。`set "hello world"` 会被切成 `set` / `"hello` / `world"`。
> 需要更复杂的解析请自己处理 `RawInput`。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/CommandProcessor.md](API/CommandProcessor.md) | `CommandProcessor`、`CommandContext`、`CommandHandler` 与全部行为细节 |
