namespace ReluctersteSDK.LlmHelper
{
    /// <summary>
    /// LLM 配置实体
    /// </summary>
    public class LlmConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    }
}
