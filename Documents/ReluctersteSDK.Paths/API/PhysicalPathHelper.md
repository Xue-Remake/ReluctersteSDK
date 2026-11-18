# API：物理文件操作

命名空间：`ReluctersteSDK.Paths.Tools`

所有方法都接受 `string` 或 `FdPath` 两种入参，内部统一走 `PathAnalyzer.Analysis` 转成绝对路径再操作磁盘。

**共同约定：不抛异常。** 非法路径、无权限、路径被同名文件占用等情况一律返回 `false` / `null` / 空列表。

---

## 成员

| 方法 | 返回 | 说明 |
|---|---|---|
| `FileExists(string filePath)` | `bool` | 文件存在且路径合法 |
| `FileExists(FdPath filePath)` | `bool` | 同上 |
| `DirectoryContainsFiles(string folderPath, bool checkSubDirectories = false)` | `bool` | 目录下**至少有一个文件**；目录不存在 → `false` |
| `DirectoryContainsFiles(FdPath folderPath, bool checkSubDirectories = false)` | `bool` | 同上 |
| `EnsureDirectoryExists(string folderPath)` | `AbsolutePath?` | 不存在则递归创建；成功返回绝对路径对象 |
| `EnsureDirectoryExists(FdPath folderPath)` | `AbsolutePath?` | 同上 |
| `GetFiles(string folderPath, bool checkSubDirectories = false, Func<string,bool>? predicate = null)` | `List<AbsolutePath>` | 枚举文件，可按绝对路径字符串过滤 |
| `GetFiles(FdPath folderPath, bool checkSubDirectories = false, Func<string,bool>? predicate = null)` | `List<AbsolutePath>` | 同上 |

---

## 行为细节

### `FileExists`

```csharp
bool exists = PhysicalPathHelper.FileExists(@"Data\save.db");
```

先 `Analysis`，路径非法直接 `false`，不会把异常泄漏出来。只判断**文件**，目录返回 `false`。

### `DirectoryContainsFiles`

```csharp
bool hasAny = PhysicalPathHelper.DirectoryContainsFiles(@"Data", checkSubDirectories: true);
```

- 目录不存在 → `false`；
- 递归时用 `SearchOption.AllDirectories`，遇到无权限子目录整体返回 `false`（异常被吞）；
- 只数**文件**，空目录（哪怕有子目录）返回 `false`。

### `EnsureDirectoryExists`

```csharp
AbsolutePath? dir = PhysicalPathHelper.EnsureDirectoryExists(new RelativePath(@"Data\Databases"));
```

- 路径非法 → `null`；
- 已存在 → 返回原绝对路径；
- 不存在 → `Directory.CreateDirectory`（自带递归创建所有上级）；
- 创建失败 → `null`。常见原因：无写权限、路径上某一级被**同名文件**占用。

返回的是 `AbsolutePath`，可以直接拿 `.PathStr` 传给别的 API：

```csharp
var db = new SqliteDataBase(dir!.PathStr, "app.db");
```

### `GetFiles`

```csharp
List<AbsolutePath> jsons = PhysicalPathHelper.GetFiles(
    @"Data",
    checkSubDirectories: false,
    predicate: p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
```

- `predicate` 的入参是**文件的绝对路径字符串**，不是 `FdPath`；
- 目录不存在 / 无权限 → 空列表（不是 `null`）；
- 返回顺序依赖 `Directory.EnumerateFiles`，**不保证排序**，需要稳定顺序请自己 `OrderBy`；
- 只返回文件，不返回目录。

---

## 已知限制

- **错误信息全部丢失**：异常被吞成 `false` / `null`，排查失败原因只能自己再调一次原生 API。
- **没有写入 / 删除 / 移动 / 复制**：本类只做"查询与建目录"，文件 I/O 请用 `System.IO`。
- **`DirectoryContainsFiles` 递归时一处失败即整体失败**：无权限的子目录会让结果变成 `false`，即使别处有文件。
- **不是原子的**：`EnsureDirectoryExists` 返回 `true` 后目录仍可能被其他进程删除。
- **`predicate` 抛异常不会被捕获**：异常发生在 `Where` 迭代里，但整个循环包在 `try/catch` 中，会被吞成"部分结果"——写谓词时不要依赖副作用。
