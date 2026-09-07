using System.Text;
using System.Text.Json;

namespace ReluctersteSDK.LlmHelper
{
    /// <summary>
    /// 通用的 LLM 请求客户端 (基于 OpenAI 兼容格式)
    /// </summary>
    public class LlmClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly LlmConfig _config;

        public LlmClient(LlmConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 1. 处理可能为空的情况，兜底默认值
            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://api.openai.com/v1"
                : _config.BaseUrl.Trim().TrimEnd('/');

            // 2. 如果用户配置时漏掉了 http:// 或 https:// 协议头，自动为其补全
            if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "https://" + baseUrl;
            }

            _http = new HttpClient();

            // 3. 赋值经过清洗的格式化 URI
            _http.BaseAddress = new Uri($"{baseUrl}/");

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        public async Task<string> SendChatAsync(string systemPrompt, string userMessage, double temperature = 0.7, double topP = 0.9)
        {
            var body = new
            {
                model = _config.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = temperature,
                top_p = topP
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        /// <summary>
        /// 验证 API 连通性与可用性
        /// </summary>
        public async Task<bool> ValidateAsync()
        {
            try
            {
                await SendChatAsync("You are a helpful assistant.", "Hi", 0.1, 0.1);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
