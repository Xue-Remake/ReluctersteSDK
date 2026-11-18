# API：LlmClient

命名空间：`ReluctersteSDK.Llm`

```csharp
public class LlmClient : IDisposable
{
    public LlmClient(LlmConfig config);
    public Task<string> SendChatAsync(string systemPrompt, string userMessage,
                                      double temperature = 0.7, double topP = 0.9);
    public Task<bool> ValidateAsync();
    public void Dispose();
}
```

---

## `LlmConfig`

```csharp
public class LlmConfig
{
    public string ApiKey  { get; set; } = string.Empty;
    public string Model   { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
```

普通 POCO，同时用作 `System.Text.Json` 的序列化目标。

---

## 构造：BaseUrl 清洗

`LlmClient(LlmConfig config)`：

1. `config` 为 `null` → 抛 `ArgumentNullException`；
2. `BaseUrl` 为空 / 空白 → `https://api.openai.com/v1`；
3. 否则 `Trim()` 并去掉末尾的 `/`；
4. **不以 `http://` / `https://` 开头时自动补 `https://`**；
5. `HttpClient.BaseAddress = new Uri($"{baseUrl}/")`；
6. 设置 `Authorization: Bearer {ApiKey}`。

```csharp
new LlmClient(new LlmConfig { Model = "deepseek-chat", BaseUrl = "api.deepseek.com/v1" });
// 实际请求 https://api.deepseek.com/v1/chat/completions
```

> 构造函数里**直接 `new HttpClient()`**，不接收外部实例。每次 `new LlmClient` 都会新建一个 `HttpClient` 和一个 socket 池。

---

## `SendChatAsync`

### 请求体

```json
{
  "model": "{LlmConfig.Model}",
  "messages": [
    { "role": "system", "content": "{systemPrompt}" },
    { "role": "user",   "content": "{userMessage}" }
  ],
  "temperature": 0.7,
  "top_p": 0.9
}
```

POST 到相对地址 `chat/completions`（相对 `BaseAddress`）。

### 响应解析

```csharp
doc.RootElement.GetProperty("choices")[0]
   .GetProperty("message")
   .GetProperty("content")
   .GetString() ?? string.Empty;
```

**硬编码只读这一条路径**：

- 不支持 `function_call` / `tool_calls`；
- 不支持多轮（`messages` 只有 system + user 两条）；
- 服务商返回非标准结构时抛 `KeyNotFoundException`（`GetProperty` 找不到就炸）。

### 异常

- 非 2xx 响应 → `EnsureSuccessStatusCode()` 抛 `HttpRequestException`；
- 不做 429 / 5xx 的区分与重试；
- 没有 `CancellationToken` 参数，调用方**无法取消**。

---

## `ValidateAsync`

```csharp
public async Task<bool> ValidateAsync()
{
    try { await SendChatAsync("You are a helpful assistant.", "Hi", 0.1, 0.1); return true; }
    catch { return false; }
}
```

发一句 `"Hi"`，**任何异常都返回 `false`**。用来做启动时的连通性检查可以，但**不区分"Key 错"和"网络断"**。

---

## `Dispose`

`Dispose()` 释放内部 `HttpClient`。用 `using var client = new LlmClient(...)` 即可。

---

## ⚠️ 上生产前必须处理

| # | 问题 | 影响 |
|---|---|---|
| 1 | **没有取消支持** | `SendChatAsync` 不接受 `CancellationToken`，UI 里点了取消也没用 |
| 2 | **没有超时配置** | 用 `HttpClient` 默认 100 秒 |
| 3 | **没有重试 / 限流处理** | `EnsureSuccessStatusCode` 直接抛，429 / 5xx 一视同仁 |
| 4 | **没有流式输出** | 一次性拿完整响应，长回答会长时间无反馈 |
| 5 | **响应解析硬编码** | 只读 `choices[0].message.content`，不支持 function calling / tool use / 多轮 |
| 6 | **直接 `new HttpClient()`** | 频繁 `new LlmClient` 有端口耗尽风险，建议长期持有一个 |
| 7 | **`ApiKey` 明文** | 不要提交到版本库，注意 `SaveToFile` 的落盘位置 |
| 8 | **只支持 `chat/completions`** | 没有 embeddings / images / audio 等其它端点 |

### 建议的改造方向

```csharp
// 1. 接注入的 HttpClient（由 IHttpClientFactory 提供）
public LlmClient(LlmConfig config, HttpClient http) { ... }

// 2. 转发 CancellationToken
public Task<string> SendChatAsync(..., CancellationToken ct = default);

// 3. 用 HttpRequestMessage + SendAsync 以便读取状态码做分支
// 4. 用 JsonDocument 的 TryGetProperty 而不是 GetProperty
```

---

## 已知限制

- **`BaseUrl` 强制补 `https://`**：本地 `http://localhost:11434` 这类地址没问题（已有协议头就不会被改），但不能用相对地址。
- **构造失败的 `Uri` 会抛异常**：`BaseUrl` 里有非法字符时 `new Uri` 直接抛，没有友好提示。
- **`ApiKey` 为空时照样发请求**：会得到 401，由 `EnsureSuccessStatusCode` 抛出。
- **没有内容审核 / 长度限制处理**：超长 prompt 直接被服务端拒绝。
