using System.Text;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 把主键列编码成字符串，供内存与 JSON 存储做字典键。
    /// 用长度前缀拼接，避免 "ab"+"c" 与 "a"+"bc" 这类碰撞。
    /// </summary>
    internal static class ConfigKey
    {
        internal static string From(IReadOnlyList<object?> keys)
        {
            var builder = new StringBuilder();
            foreach (object? key in keys)
            {
                string text = key?.ToString() ?? "\0";
                builder.Append(text.Length).Append(':').Append(text);
            }
            return builder.ToString();
        }

        internal static string From(object? key) => From(new object?[] { key });
    }
}
