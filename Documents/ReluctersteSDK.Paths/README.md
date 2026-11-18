# ReluctersteSDK.Paths

把"路径"从裸字符串提升为**带语义的对象**：绝对路径与相对路径是两种类型，路径中的每一段是 `PathSegment`，合法性在构造时校验。

- 目录：`ReluctersteSDK.Paths/`
- 包 ID：`ReluctersteSDK.Paths`
- 命名空间：`ReluctersteSDK.Paths`（工具类在 `ReluctersteSDK.Paths.Tools`）
- 依赖：无

---

## 场景一：把用户输入的路径变成对象

**目标**：不知道用户给的是绝对还是相对，交给库自己判断。

```csharp
using ReluctersteSDK.Paths;

FdPath p = FdPath.Parse(@"Data\config.json");      // → RelativePath，PathStr 为 ".\Data\config.json"
FdPath q = FdPath.Parse(@"E:\Work\Data\x.json");   // → AbsolutePath
```

也可以显式构造：

```csharp
var abs = new AbsolutePath(@"E:\Work\Data\x.json");
var rel = new RelativePath(@"Data\Databases");     // 自动补 "." → ".\Data\Databases"
```

> `RelativePath` 是**类型**上的相对——`new RelativePath(...)` 无论传什么都按相对路径解释。

---

## 场景二：拼接基础目录和文件名

**目标**：手里有一个目录对象，要得到它下面的某个文件。

```csharp
FdPath basePath = FdPath.Parse(@"Data");
FdPath full = basePath * "config\settings.json";

full.PathStr;   // ".\Data\config\settings.json"
```

`*` 会自动处理 `.` 与 `..`：

```csharp
FdPath.Parse(@"A\B") * @"..\C";    // .\A\C
FdPath.Parse(@"A") * @".\B";       // .\A\B
FdPath.Parse(@"A") * @"E:\abs";    // E:\abs —— 右侧是绝对路径时直接取右侧
```

---

## 场景三：算出两个路径的相对关系

**目标**：已知目标文件和基准目录，得到它们之间的相对路径。

```csharp
FdPath target  = new AbsolutePath(@"E:\Work\A\B\C.txt");
FdPath baseDir = new AbsolutePath(@"E:\Work\A");

RelativePath rel = target / baseDir;   // .\B\C.txt
```

同一目录下会得到 `.\xxx`，不会返回空串；**跨盘符会抛 `InvalidOperationException`**。

---

## 场景四：校验后再使用外部输入

**目标**：配置文件里读出来的路径可能是垃圾，先验一遍。

```csharp
using ReluctersteSDK.Paths.Tools;

if (PathValidator.IsValidPathFormat(userInput))
{
    FdPath p = FdPath.Parse(userInput);
}

// 或一步到位：合法返回绝对路径，非法返回 null
AbsolutePath? abs = PathAnalyzer.Analysis(userInput);
if (abs is null)
{
    Console.WriteLine("路径非法");
}
```

`Analysis` 不抛异常，失败返回 `null`，适合直接贴在输入处理里。

---

## 场景五：确保存档目录存在，并列出所有 json

```csharp
using ReluctersteSDK.Paths.Tools;

AbsolutePath? dir = PhysicalPathHelper.EnsureDirectoryExists(new RelativePath(@"Data\Databases"));
if (dir is null)
{
    Console.WriteLine("目录创建失败（无权限 / 被同名文件占用）");
    return;
}

List<AbsolutePath> jsons = PhysicalPathHelper.GetFiles(
    @"Data",
    checkSubDirectories: false,
    predicate: p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

foreach (AbsolutePath file in jsons)
    Console.WriteLine(file.PathStr);
```

所有物理操作都**吞掉异常**，失败时返回 `false` / `null` / 空列表，不会中断流程。

---

## 场景六：把路径当作字典键

**目标**：缓存"每个文件对应什么"，不想自己处理大小写。

```csharp
var cache = new Dictionary<FdPath, DateTime>();

cache[new AbsolutePath(@"E:\Work\A.txt")] = DateTime.UtcNow;

// 大小写不同、末尾多一个分隔符，都能命中
bool hit = cache.ContainsKey(new AbsolutePath(@"e:\work\a.txt\"));
```

相等性基于**格式化后的字符串 + `OrdinalIgnoreCase`**，所以 `FdPath` 可以直接当键用。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/FdPath.md](API/FdPath.md) | `FdPath` / `AbsolutePath` / `RelativePath` / `PathSegment`：类型体系、`*` 与 `/` 的精确规则、相等性 |
| [API/Validation.md](API/Validation.md) | `PathValidator`（合法性校验与格式化）、`PathAnalyzer`（相对 ↔ 绝对） |
| [API/PhysicalPathHelper.md](API/PhysicalPathHelper.md) | 文件存在性、目录创建、文件枚举 |
