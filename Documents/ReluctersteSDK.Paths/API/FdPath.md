# API：路径类型体系

命名空间：`ReluctersteSDK.Paths`

```
FdPath（abstract）
├─ AbsolutePath   "E:\Work\Data\x.json"
└─ RelativePath   ".\Data\x.json"

PathSegment       路径中的一段：Name / Type / Kind
PathSegmentKind   Directory | File | Root | Current | Parent
```

---

## `FdPath`（抽象基类）

由 `PathSegment` 序列驱动，本身不能实例化。

| 成员 | 类型 | 说明 |
|---|---|---|
| `Segments` | `IReadOnlyList<PathSegment>` | 路径元序列（构造时 `ToArray` 复制，之后不可变） |
| `PathStr` | `string` | 等价于 `ToString()` |
| `static Parse(string path)` | `FdPath` | **自动识别**：合法绝对路径 → `AbsolutePath`，否则 → `RelativePath` |
| `static operator *(FdPath, FdPath)` | `FdPath` | 拼接 |
| `static operator *(FdPath, string)` | `FdPath` | 拼接 |
| `static operator *(string, FdPath)` | `FdPath` | 拼接 |
| `static operator /(FdPath, FdPath)` | `RelativePath` | 求相对路径 |
| `static operator /(FdPath, string)` | `RelativePath` | 求相对路径 |
| `static operator /(string, FdPath)` | `RelativePath` | 求相对路径 |
| `Equals(FdPath?)` | `bool` | 按 `PathStr` 比较，`OrdinalIgnoreCase` |
| `GetHashCode()` | `int` | 按 `PathStr` 的 `OrdinalIgnoreCase` 哈希 |
| `abstract ToString()` | `string` | 由子类决定格式 |

> `Parse` 判定绝对路径用的是 `PathValidator.IsValidAbsolutePath`，**不是** `Path.IsPathRooted`。`C:` 这种驱动器相对路径会被判为**相对路径**。

---

## `RelativePath`

| 构造 / 成员 | 说明 |
|---|---|
| `RelativePath(string path)` | 解析字符串 |
| `RelativePath(IEnumerable<PathSegment> segments)` | 直接给片段 |
| `static ParseStringToSegments(string path, bool isAbsolute)` | 底层解析器，`isAbsolute: false` |
| `ToString()` | 空 → `"."`；否则用 `Path.DirectorySeparatorChar` 连接各段 `Name` |

### 规范化规则

`ParseStringToSegments(path, isAbsolute: false)`：

1. 空白输入 → 空列表（`ToString()` 得 `"."`）。
2. 按 `\` 和 `/` 切分，**丢弃空段**（`"A//B"` 等价于 `"A/B"`）。
3. 若首段不是 `.` 或 `..`，**自动插入 `.` 作为首段**。

```csharp
new RelativePath("A").PathStr;      // ".\A"
new RelativePath(@".\A").PathStr;   // ".\A"
new RelativePath(@"..\A").PathStr;  // "..\A"
new RelativePath("").PathStr;       // "."
```

因此**相对路径无法表达"无前缀"**——这是有意为之，避免 `A` 与 `.\A` 两种写法产生两个不同的对象。

---

## `AbsolutePath`

| 构造 / 成员 | 说明 |
|---|---|
| `AbsolutePath(string path)` | 解析后**立即绝对化**（见下） |
| `AbsolutePath(IEnumerable<PathSegment> segments)` | 直接给片段（首段应为 `Root`） |
| `ToString()` | 盘符后自动补分隔符；根之后的段用 `Path.DirectorySeparatorChar` 连接 |

### 构造时会重写路径

`AbsolutePath(string)` 内部先调用 `PathValidator.ToAbsolutePath(path)`，再拆分：

```csharp
new AbsolutePath(@"Data\x.json").PathStr;
// = Path.GetFullPath(@"Data\x.json", AppContext.BaseDirectory)
// 例如 "E:\...\bin\Debug\net10.0\Data\x.json"
```

**基准是 `AppContext.BaseDirectory`**（程序所在目录），不是 `Environment.CurrentDirectory`（进程工作目录）。单文件发布、单元测试、从快捷方式启动时这两者经常不同。

已传入绝对路径时结果不变，只在格式上做规范化（`/` → `\`、去重分隔符等）。

---

## `PathSegment`

一个路径段。

| 属性 | 类型 | 说明 |
|---|---|---|
| `Name` | `string` | 原始名称，如 `"A"`、`"doc.txt"`、`"C:"`、`".."` |
| `Type` | `string` | 目录 → `"folder"`（`PathSegment.FolderType`）；文件 → 扩展名（含点，如 `".txt"`） |
| `Kind` | `PathSegmentKind` | 语义种类 |

| 构造 / 成员 | 说明 |
|---|---|
| `PathSegment(string name, string type, PathSegmentKind kind)` | `name` 为 null 抛 `ArgumentNullException`；`type` 为 null 视为 `""` |
| `static Create(string rawSegment, bool isLastSegment, bool hasTrailingSlash)` | 从原始字符串推断 |
| `static FolderType` | 常量 `"folder"` |
| `Equals` | `Name` 与 `Type` 均 `OrdinalIgnoreCase`，`Kind` 相等 |
| `GetHashCode` | `Name`（忽略大小写）+ `Kind` |

### `Create` 的判定顺序

| 条件 | 结果 |
|---|---|
| 匹配 `^[a-zA-Z]:\\?$` | `Kind = Root`，`Type = "folder"` |
| 以 `\\` 或 `//` 开头 | `Kind = Root`（UNC 根），`Type = "folder"` |
| `"."` | `Kind = Current` |
| `".."` | `Kind = Parent` |
| 非最后一段，或路径以分隔符结尾 | `Kind = Directory` |
| 最后一段且 `Path.GetExtension` 非空 | `Kind = File`，`Type = 扩展名` |
| 最后一段且无扩展名 | `Kind = Directory`（**默认当目录**） |

> **无扩展名的文件无法用字符串表达**。需要时直接构造：
> `new PathSegment("Makefile", "", PathSegmentKind.File)`。

---

## `*` 拼接的精确规则

`Combine(left, right)`：

1. **`right` 是 `AbsolutePath` → 直接返回 `right`**（与 `Path.Combine` 行为一致）。
2. 以 `left.Segments` 为起点，逐段消费 `right.Segments`：
   - `Current`（`.`）→ 跳过；
   - `Parent`（`..`）→ 若上一个是普通段则弹出它，否则（栈空 / 上一个是 `Root` 或 `Parent`）保留 `..`；
   - 其他 → 追加。
3. 结果类型跟随 `left`：`left` 是绝对 → `AbsolutePath`，否则 `RelativePath`。

```csharp
FdPath.Parse(@"A\B") * @"..\C";   // .\A\C
FdPath.Parse(@"A")   * @"..\B";   // .\B
FdPath.Parse(@"A")   * @".\B";    // .\A\B
FdPath.Parse(@"A")   * @"E:\abs"; // E:\abs
```

弹到只剩 `.` 时 `..` 会把 `.` 也弹掉，结果退化成 `.`——因为 `.` 本身就是空路径的表示。

注意 `*` **不检查文件系统**，纯粹是路径代数。

---

## `/` 求相对路径的精确规则

`MakeRelative(target, base)`：

1. 两边先 `NormalizeSegments`：去掉 `.`，折叠可消解的 `..`（不越过根）。
2. 若两边首段都是 `Root` 且名字不同 → **抛 `InvalidOperationException`**（跨盘符 / 跨 UNC 根）。
3. 找公共前缀长度 `commonLength`（按 `Name` 忽略大小写）。
4. `base` 剩余段数 = `upSteps`，先放 `upSteps` 个 `..`。
5. 追加 `target` 从 `commonLength` 起的剩余段。
6. 若 `upSteps == 0`，在最前面补一个 `.`（保证结果带相对前缀）。

```csharp
new AbsolutePath(@"E:\Work\A\B\C.txt") / new AbsolutePath(@"E:\Work\A");  // .\B\C.txt
new AbsolutePath(@"E:\Work\A")         / new AbsolutePath(@"E:\Work\A");  // .
new AbsolutePath(@"E:\Work")           / new AbsolutePath(@"E:\Work\A");  // ..
```

绝对 vs 相对混算不会报错——`NormalizeSegments` 只按段比较，首段类型不同自然公共前缀为 0，结果是 `..\..\...` 形式。**跨盘符才是硬错误**。

---

## 已知限制

- **相对路径无法表达"无前缀"**：`new RelativePath("A")` 一定是 `.\A`。
- **空段被丢弃**：`"A//B"` 与 `"A/B"` 等价。
- **无扩展名文件被判为目录**：见 `PathSegment.Create` 判定表。
- **基准目录固定**：绝对化的基准是 `AppContext.BaseDirectory`，不可配置。
- **`Equals` 依赖格式化后的字符串**：末尾分隔符、空段、`.` 都会被规范化掉，所以 `E:\Work\A` 与 `e:\work\a\` 相等；但 `\` 与 `/` 之外的书写差异（例如大小写以外的符号）不会被自动统一。
- **相等性忽略大小写**：在区分大小写的文件系统（Linux）上会误判为同一个路径。
- **不做文件系统查询**：`*`、`/`、`Equals` 都不看磁盘。
