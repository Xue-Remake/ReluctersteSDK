using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReluctersteSDK.Registry.Json
{
    /// <summary>
    /// <see cref="CompositeKey"/> 的 JSON 转换器。
    /// 序列化形式为一个 JSON 数组，元素即复合键的各列值，例如 ["player", 1, true]。
    /// </summary>
    /// <remarks>
    /// 反序列化时会把 JSON 标量还原为 CLR 基元类型：
    /// 字符串→<see cref="string"/>，true/false→<see cref="bool"/>，
    /// 数字优先还原为 <see cref="int"/>、其次 <see cref="long"/>、再次 <see cref="double"/>，
    /// 数组/对象则保留为 <see cref="JsonElement"/>，null 保留为 <c>null</c>。
    /// 由于 <see cref="CompositeKey"/> 基于值相等比较，请确保
    /// 注册时使用的键类型与反序列化后还原的类型保持一致。
    /// </remarks>
    public sealed class CompositeKeyConverter : JsonConverter<CompositeKey>
    {
        /// <summary>读取一个复合键</summary>
        /// <exception cref="JsonException">令牌类型不是数组时抛出</exception>
        public override CompositeKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"CompositeKey 需要以 JSON 数组表示，实际令牌为 {reader.TokenType}。");

            var keys = new List<object?>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                keys.Add(ReadKeyColumn(ref reader));
            }
            return new CompositeKey(keys.ToArray());
        }

        /// <summary>写出一个复合键</summary>
        public override void Write(Utf8JsonWriter writer, CompositeKey value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Length; i++)
            {
                WriteKeyColumn(writer, value[i], options);
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// 直接读取光标处的复合键（不依赖选项中是否已注册本转换器），供其他转换器内部复用。
        /// </summary>
        internal static CompositeKey ReadDirect(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"CompositeKey 需要以 JSON 数组表示，实际令牌为 {reader.TokenType}。");

            var keys = new List<object?>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                keys.Add(ReadKeyColumn(ref reader));
            }
            return new CompositeKey(keys.ToArray());
        }

        /// <summary>
        /// 直接写出一个复合键（不依赖选项中是否已注册本转换器），供其他转换器内部复用。
        /// </summary>
        internal static void WriteDirect(Utf8JsonWriter writer, CompositeKey value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Length; i++)
            {
                WriteKeyColumn(writer, value[i], options);
            }
            writer.WriteEndArray();
        }

        private static object? ReadKeyColumn(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var intValue)) return intValue;
                    if (reader.TryGetInt64(out var longValue)) return longValue;
                    if (reader.TryGetDouble(out var doubleValue)) return doubleValue;
                    return reader.GetDecimal();
                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                    using (var document = JsonDocument.ParseValue(ref reader))
                    {
                        return document.RootElement.Clone();
                    }
                default:
                    throw new JsonException($"不支持作为复合键列的令牌类型：{reader.TokenType}。");
            }
        }

        private static void WriteKeyColumn(Utf8JsonWriter writer, object? column, JsonSerializerOptions options)
        {
            if (column is null)
            {
                writer.WriteNullValue();
                return;
            }
            if (column is JsonElement element)
            {
                element.WriteTo(writer);
                return;
            }
            JsonSerializer.Serialize(writer, column, column.GetType(), options);
        }
    }
}
