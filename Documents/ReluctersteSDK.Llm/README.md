# ReluctersteSDK.Llm

面向 **OpenAI 兼容接口**（`/chat/completions`）的最小 LLM 客户端，外加一个"按模型名猜服务商地址"的配置管理器。

- 目录：`ReluctersteSDK.Llm/`
- 包 ID：`ReluctersteSDK.Llm`
- 命名空间：`ReluctersteSDK.Llm`
- 依赖：无（用 `HttpClient` 与 `System.Text.Json`）

---

## 场景一：发一条消息，拿回复

```csharp
using ReluctersteSDK.Llm;

var config = new LlmConfig
{
    ApiKey  = "sk-xxxxxxxx",
    Model   = "deepseek-chat",
    BaseUrl = "https://api.deepseek.com/v1"
};

using var client = new LlmClient(config);

string answer = await client.SendChatAsync(
    systemPrompt: "你是一个严谨的助手。",
    userMessage:  "用一句话解释什么是事件总线。",
    temperature:  0.7,
    topP:         0.9);
```

`BaseUrl` 可以被清洗：

| 输入 | 实际使用 |
|---|---|
| 空 | `https://api.openai.com/v1` |
| `api.deepseek.com/v1` | 自动补成 `https://api.deepseek.com/v1` |
| `https://api.deepseek.com/v1/` | 去掉末尾 `/` |

---

## 场景二：先验证 Key 能不能用

```csharp
if (!await client.ValidateAsync())
{
    Console.WriteLine("API Key 或网络有问题");
    return;
}
```

`ValidateAsync` 发一句 `"Hi"`，成功返回 `true`，**任何异常返回 `false`**（不抛）。

---

## 场景三：从配置文件读 Key

```csharp
using ReluctersteSDK.Llm;

LlmConfig? cfg = LlmConfigManager.LoadFromFile(@"config\llm.json");
if (cfg is null)
{
    Console.WriteLine("配置读不出来");
    return;
}
```

兼容两种格式：

```jsonc
// 1. 标准对象
{ "ApiKey": "sk-...", "Model": "deepseek-chat", "BaseUrl": "https://api.deepseek.com/v1" }

// 2. 旧项目的数组格式（只有 key + model，地址自动推断）
[ "sk-...", "deepseek-chat" ]
```

---

## 场景四：只给模型名，地址自动推断

```csharp
LlmConfigManager.GuessBaseUrl("deepseek-chat");   // https://api.deepseek.com/v1
LlmConfigManager.GuessBaseUrl("qwen-plus");       // https://dashscope.aliyuncs.com/compatible-mode/v1
LlmConfigManager.GuessBaseUrl("unknown-model");   // null
```

内置的服务商前缀见 [API/LlmConfigManager.md](API/LlmConfigManager.md#guessbaseurl)。

---

## 场景五：保存配置

```csharp
LlmConfigManager.SaveToFile(cfg, @"config\llm.json");
```

写的是标准对象格式（`WriteIndented`），**不写数组格式**。

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/LlmClient.md](API/LlmClient.md) | 构造、`SendChatAsync`、`ValidateAsync`、请求体格式、上生产前必须处理的限制 |
| [API/LlmConfigManager.md](API/LlmConfigManager.md) | 文件读写、`GuessBaseUrl` 的前缀表与匹配规则 |
