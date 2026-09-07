using System.Text.Json;

namespace ReluctersteSDK.LlmHelper
{
    /// <summary>
    /// LLM 配置管理器，负责加载配置和智能识别 API 地址
    /// </summary>
    public static class LlmConfigManager
    {
        // 模型名前缀 -> 默认 Base URL 映射表
        private static readonly Dictionary<string, string> KnownProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            { "gpt-",      "https://api.openai.com/v1" },
            { "o1-",       "https://api.openai.com/v1" },
            { "o3-",       "https://api.openai.com/v1" },
            { "deepseek-", "https://api.deepseek.com/v1" },
            { "qwen-",     "https://dashscope.aliyuncs.com/compatible-mode/v1" },
            { "qwq-",      "https://dashscope.aliyuncs.com/compatible-mode/v1" },
            { "ernie-",    "https://qianfan.baidubce.com/v2" },
            { "glm-",      "https://open.bigmodel.cn/api/paas/v4" },
            { "chatglm-",  "https://open.bigmodel.cn/api/paas/v4" },
            { "moonshot-", "https://api.moonshot.cn/v1" },
            { "yi-",       "https://api.lingyiwanwu.com/v1" },
            { "abab",      "https://api.minimax.chat/v1" },
            { "step-",     "https://api.stepfun.com/v1" }
        };

        /// <summary>
        /// 根据模型名称自动推断 Base URL
        /// </summary>
        public static string? GuessBaseUrl(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return null;
            foreach (var kv in KnownProviders)
            {
                if (model.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>
        /// 从 JSON 文件中加载配置 (兼容原数组格式或标准对象格式)
        /// </summary>
        public static LlmConfig? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var jsonContent = File.ReadAllText(filePath);

                // 兼容原项目的数组格式 ["apiKey", "modelName"]
                if (jsonContent.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<string[]>(jsonContent);
                    if (arr != null && arr.Length >= 2)
                    {
                        var model = arr[1].Trim();
                        return new LlmConfig
                        {
                            ApiKey = arr[0].Trim(),
                            Model = model,
                            BaseUrl = GuessBaseUrl(model) ?? "https://api.openai.com/v1"
                        };
                    }
                }
                else
                {
                    // 标准的 JSON 对象格式
                    return JsonSerializer.Deserialize<LlmConfig>(jsonContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 将配置保存到文件
        /// </summary>
        public static void SaveToFile(LlmConfig config, string filePath)
        {
            var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonContent);
        }
    }
}
