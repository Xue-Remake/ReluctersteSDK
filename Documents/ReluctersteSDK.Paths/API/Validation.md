# API：校验与相对化

命名空间：`ReluctersteSDK.Paths.Tools`

- [`PathValidator`](#pathvalidator)：合法性校验、格式化、绝对化
- [`PathAnalyzer`](#pathanalyzer)：任意字符串 ↔ `FdPath`，相对 ↔ 绝对

---

## `PathValidator`

### 成员

| 方法 | 返回 | 说明 |
|---|---|---|
| `IsValidPathFormat(string path)` | `bool` | 绝对或相对路径是否合法 |
| `IsValidAbsolutePath(string path)` | `bool` | 是否**严格**绝对路径 |
| `FormatPath(string path)` | `string?` | 合法则规范化为 `\` 分隔；非法返回 `null` |
| `TryFormatPath(string path, out string? formattedPath)` | `bool` | 同上，用返回值表示成败 |
| `ToAbsolutePath(string relativePath)` | `string` | 基于 `AppContext.BaseDirectory` 转绝对字符串，**不做合法性校验** |

### 校验内容

三条 `Is*` / `Format*` 方法共用同一套检查，按顺序：

1. 空白字符串 → 非法。
2. 含 `Path.GetInvalidPathChars()` → 非法。
3. `Path.GetFullPath` 抛 `ArgumentException` / `NotSupportedException` / `PathTooLongException` → 非法。
4. Windows 下的根格式：
   - UNC（`\\` 或 `//` 开头）至少要有 2 段（`\\server\share`），否则非法；
   - 盘符根长度不得超过 3，首字符必须是字母、第二个必须是 `:`、第三位必须是分隔符；
   - 其它根必须恰好是 `\` 或 `/`。
5. 逐段检查（按 `\` 与 `/` 切开）：
   - 空白段 → 非法；
   - 单段长度 > 255 → 非法；
   - 含 `Path.GetInvalidFileNameChars()` → 非法；
   - Windows 保留名 → 非法（取第一个 `.` 前的主名，大写比较）。
6. 根剥离成功才继续；`IsValidAbsolutePath` 在这方面更严格，剥离失败直接判非法。

保留名清单：`CON` `PRN` `AUX` `NUL` `COM1`–`COM9` `LPT1`–`LPT9`。

### `IsValidPathFormat` vs `IsValidAbsolutePath`

两者的差别**只在根的处理**：

| 输入 | `IsValidPathFormat` | `IsValidAbsolutePath` |
|---|---|---|
| `E:\Work\x.json` | ✅ | ✅ |
| `Data\x.json` | ✅ | ❌（根为空） |
| `C:` | ✅（根长度 ≤ 3） | ❌（驱动器的"当前目录"，不是绝对路径） |
| `\Temp` | ✅ | ❌（当前驱动器根，Windows 语义上是相对） |
| `C::\x` | ❌（根长度 > 3） | ❌ |
| `\\server\share\x` | ✅ | ✅ |
| `\\server` | ❌（UNC 段数不足） | ❌ |
| `CON.txt` | ❌（保留名） | ❌ |

```csharp
PathValidator.IsValidAbsolutePath("C:");        // false
PathValidator.IsValidPathFormat(@"CON.txt");    // false
```

### `FormatPath` 的规范化规则

合法时：

1. 根里的 `/` 全部换成 `\`；UNC 根重建为 `\\server\share` 形式。
2. 主体按段重新拼接，分隔符统一为 `\`。
3. **相对路径若首段不是 `.` 或 `..`，前置 `.\`**。
4. **输入末尾有分隔符时，输出也保留末尾 `\`**。

```csharp
PathValidator.FormatPath("Data/Databases/");    // ".\Data\Databases\"
PathValidator.FormatPath(@"E:/Work//A");        // "E:\Work\A"
PathValidator.FormatPath(@"..\A");              // "..\A"
PathValidator.FormatPath("C:/Users/");          // "C:\Users\"
```

注意第 4 条：`FormatPath` 保留末尾分隔符，而 `RelativePath` / `AbsolutePath` 的 `ToString()` **不保留**。两者格式不同，不要混用比较。

### `ToAbsolutePath`

```csharp
public static string ToAbsolutePath(string relativePath)
    => Path.GetFullPath(relativePath, AppContext.BaseDirectory);
```

**不做校验**，传入非法字符串会抛异常。需要校验请先走 `FormatPath` / `Analysis`。

基准是 `AppContext.BaseDirectory`（程序集所在目录），**不是** `Environment.CurrentDirectory`。

---

## `PathAnalyzer`

### 成员

| 方法 | 返回 | 说明 |
|---|---|---|
| `Analysis(string str)` | `AbsolutePath?` | 校验 + 格式化 + 绝对化；非法返回 `null` |
| `Analysis(FdPath path)` | `AbsolutePath?` | 已是 `AbsolutePath` 时**原样返回**，否则按 `PathStr` 绝对化（不做校验） |
| `ToRelative(string str)` | `RelativePath?` | 相对于 `AppContext.BaseDirectory`；非法或跨盘符返回 `null` |
| `ToRelative(AbsolutePath path)` | `RelativePath?` | 等价于 `ToRelative(path.PathStr)` |

### 行为

```csharp
using ReluctersteSDK.Paths.Tools;

AbsolutePath? abs = PathAnalyzer.Analysis(@"Data\save.db");
// = new AbsolutePath(Path.GetFullPath(@"Data\save.db", AppContext.BaseDirectory))

RelativePath? rel = PathAnalyzer.ToRelative(@"E:\Work\Data\save.db");
// 相对于 AppContext.BaseDirectory 计算；跨盘符 → null
```

- `Analysis(string)` **不抛异常**，任何非法输入都返回 `null`。
- `Analysis(FdPath)` 若传入相对路径，走 `ToAbsolutePath(PathStr)`，**跳过校验**——`FdPath` 由构造函数保证已解析，但并不会拦截非法字符。
- `ToRelative` 用 `Path.GetRelativePath` 计算，结果再包成 `RelativePath`（顺带补上 `.` 前缀）。
- 目标与基准同一目录时，`ToRelative` 返回 `.`（因为 `Path.GetRelativePath` 返回 `"."`）。

### 用哪一个

| 场景 | 用 |
|---|---|
| 处理用户输入 / 配置文件里的路径 | `PathAnalyzer.Analysis`（失败返回 `null`） |
| 只想判断格式对不对 | `PathValidator.IsValidPathFormat` |
| 需要"绝对路径格式"的严格校验 | `PathValidator.IsValidAbsolutePath` |
| 想把绝对路径显示成相对路径 | `PathAnalyzer.ToRelative` |

---

## 已知限制

- **`ToAbsolutePath` 不校验**：传入垃圾字符串会抛 `ArgumentException`，调用方要自己保证输入合法。
- **`FormatPath` 与 `FdPath.ToString()` 格式不一致**：前者保留末尾 `\`，后者不保留。
- **严格绝对路径判定不看盘符是否存在**：`Z:\a` 在没有 Z 盘的机器上仍然判定为合法。
- **保留名检查只覆盖 Windows 传统设备名**：不包含 `COM10+`、`CONIN$` 等扩展名。
- **`ToRelative` 依赖进程的基准目录**，单元测试与单文件发布场景下结果会不同。
