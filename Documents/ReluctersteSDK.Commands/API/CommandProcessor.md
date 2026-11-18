# API：CommandProcessor

命名空间：`ReluctersteSDK.Commands`

---

## `CommandContext`

```csharp
public record CommandContext(string RawInput, string[] Args);
```

| 成员 | 说明 |
|---|---|
| `RawInput` | 用户输入的整行（**未做任何裁剪**） |
| `Args` | 按空格分词后的参数；第一个触发词已被切掉 |

分词实现是 `rawInput.Split(' ', StringSplitOptions.RemoveEmptyEntries)`：

- 只按**半角空格**切；
- 连续空格被折叠；
- 制表符**不**作为分隔符（会留在参数里）。

---

## `CommandProcessor`

```csharp
public class CommandProcessor
{
    public delegate Task CommandHandler(CommandContext context);

    public Func<string, Task>? OnUnknownCommand { get; set; }

    public CommandProcessor Register(string trigger, string description, CommandHandler handler, params string[] aliases);
    public CommandProcessor Register(string trigger, string description, Action<CommandContext> handler, params string[] aliases);
    public bool Disable(string triggerOrAlias);
    public bool Enable(string triggerOrAlias);
    public bool Unregister(string triggerOrAlias);
    public CommandProcessor RegisterHelpCommand();
    public Task ExecuteAsync(string? rawInput);
}
```

### `Register`

- 两个重载：`CommandHandler`（`Task` 返回）与 `Action<CommandContext>`（同步）；
- **返回 `this`**，可链式；
- 主触发词与每个别名都会成为内部字典的键，**都指向同一个命令定义**；
- 重复注册同一触发词 → **后者覆盖前者**（不报错）。

### `Disable` / `Enable`

按主键或别名查找，改的是**共享的命令定义**上的 `IsEnabled`，所以**所有别名会一起生效**。

找不到返回 `false`。

### `Unregister`

按主键或别名找到定义后，删除 `Trigger` 与**全部别名**。找不到返回 `false`。

### `RegisterHelpCommand`

注册内置命令 `help`，别名 `?` / `h`。输出按 `Trigger` 排序的命令列表，禁用的命令用深灰显示并标注 `[已禁用]`。

### `ExecuteAsync`

处理流程：

1. `null` / 空白 → 直接返回；
2. 按空格切分，空数组 → 返回；
3. 第一个词作为命令名，其余作为 `Args`；
4. 字典命中：
   - 命令被禁用 → 深黄字提示并**返回**（不走 `OnUnknownCommand`）；
   - 否则构造 `CommandContext` 并 `await` 处理器；
   - 处理器抛异常 → 红字打印 `[Error] 执行命令 'xxx' 时出错: {Message}`，**异常被吞掉**；
5. 未命中：
   - 有 `OnUnknownCommand` → `await` 它（**它的异常不会被吞**）；
   - 否则黄字提示。

---

## 行为速查

| 场景 | 行为 |
|---|---|
| 空输入 / 全空白 | 直接返回 |
| 触发词大小写 | 字典 `OrdinalIgnoreCase`，**不区分大小写** |
| 处理器抛异常 | 捕获并红字打印 `[Error] ...`，不中断循环 |
| 未知命令 | 走 `OnUnknownCommand`；未设置时黄字提示 |
| 重复注册同一触发词 | **后者覆盖前者** |
| `Disable("q")` | 联动影响 `quit` 的所有别名 |
| 被禁用的命令 | 打印提示后返回，不执行 |

---

## 已知限制

- **非线程安全**：内部只有一个 `Dictionary`，不支持并发执行命令。
- **没有参数类型转换**：没有必选/可选参数、没有 `int` 自动解析，全靠处理器自己解析 `Args`。
- **只按半角空格分词**：不支持引号、转义、制表符、管道。
- **没有命令历史 / Tab 补全**。
- **`CommandHandler` 异常被吞**：调用方无法得知失败，只能看控制台输出。
- **重复注册会留下孤儿别名**：`Register("a", ..., "x")` 后再 `Register("a", ..., "y")`，`x` 仍指向旧定义（旧定义的 `Trigger` 也是 `a`），`Unregister("a")` 会按旧定义删除，`y` 可能残留。
- **`help` 列表是快照输出**：不刷新、不交互。
- **`RegisterHelpCommand` 可以被覆盖**：之后再 `Register("help", ...)` 会顶掉它，但 `?` / `h` 仍指向旧定义。
