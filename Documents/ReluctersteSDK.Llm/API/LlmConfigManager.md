# API：LlmConfigManager

命名空间：`ReluctersteSDK.Llm`

```csharp
public static class LlmConfigManager
{
    public static string? GuessBaseUrl(string model);
    public static LlmConfig? LoadFromFile(string filePath);
    public static void SaveToFile(LlmConfig config, string filePath);
}
```

---

## `GuessBaseUrl`

```csharp
public static string? GuessBaseUrl(string model)
```

按**字典插入顺序**做 `StartsWith`（`OrdinalIgnoreCase`）匹配；全部不匹配返回 `null`。

| 前缀 | 服务商 | BaseUrl |
|---|---|---|
| `gpt-` / `o1-` / `o3-` | OpenAI | `https://api.openai.com/v1` |
| `deepseek-` | DeepSeek | `https://api.deepseek.com/v1` |
| `qwen-` / `qwq-` | 阿里云百炼 | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| `ernie-` | 百度千帆 | `https://qianfan.baidubce.com/v2` |
| `glm-` / `chatglm-` | 智谱 | `https://open.bigmodel.cn/api/paas/v4` |
| `moonshot-` | 月之暗面 | `https://api.moonshot.cn/v1` |
| `yi-` | 零一万物 | `https://api.lingyiwanwu.com/v1` |
| `abab` | MiniMax | `https://api.minimax.chat/v1` |
| `step-` | 阶跃星辰 | `https://api.stepfun.com/v1` |

### 匹配规则的两个坑

1. **是 `StartsWith`，不是最长前缀匹配**：字典顺序决定结果。新增服务商时注意别让短前缀吃掉长前缀。
2. **`abab` 没有连字符**：`abab5.5-chat` 能命中，但如果将来加了一个 `ab` 前缀，顺序会决定谁赢。

---

## `LoadFromFile`

```csharp
public static LlmConfig? LoadFromFile(string filePath)
```

文件不存在 → `null`（不抛）。

### 两种格式

**标准对象**（推荐）：

```json
{ "ApiKey": "sk-...", "Model": "deepseek-chat", "BaseUrl": "https://api.deepseek.com/v1" }
```

走 `JsonSerializer.Deserialize<LlmConfig>`，缺字段用默认值（`BaseUrl` 默认 `https://api.openai.com/v1`）。

**旧数组格式**：

```json
[ "sk-...", "deepseek-chat" ]
```

规则：

- 内容 `TrimStart()` 后以 `[` 开头才走这条分支；
- 反序列化成 `string[]`，**长度 ≥ 2** 才有效；
- `ApiKey = arr[0].Trim()`、`Model = arr[1].Trim()`；
- `BaseUrl = GuessBaseUrl(model) ?? "https://api.openai.com/v1"`。

长度不足 2 时函数落到末尾的 `return null`。

### 异常处理

整个读取包在 `try/catch` 里：**任何异常都 `Console.WriteLine` 一句然后返回 `null`**。

这包括文件被占用、JSON 语法错、数组元素为 `null`（`arr[0].Trim()` 会 NRE）。

---

## `SaveToFile`

```csharp
public static void SaveToFile(LlmConfig config, string filePath)
```

```csharp
var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(filePath, jsonContent);
```

- 写标准对象格式（**不会写数组格式**）；
- `WriteIndented = true`；
- **不创建目录**：目标目录不存在会抛 `DirectoryNotFoundException`；
- **不处理异常**：所有 IO 异常直接向上抛；
- 编码用 `File.WriteAllText` 默认（UTF-8 无 BOM）。

---

## 已知限制

- **`LoadFromFile` 用相对路径时以进程工作目录为基准**（`File.Exists` / `File.ReadAllText`），不是 `AppContext.BaseDirectory`。和 `Paths` 的基准不一致。
- **数组格式只取前两项**：多出来的元素被忽略。
- **`SaveToFile` 不建目录**：`LoadFromFile` 也不建，两个方向不对称。
- **没有加密 / 环境变量兜底**：`ApiKey` 明文落盘。
- **`LoadFromFile` 的错误只打到控制台**：无法通过返回值区分"文件不存在"和"JSON 坏了"。
- **`GuessBaseUrl` 的表是硬编码的**：不支持配置化。
