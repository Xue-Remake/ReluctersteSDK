using System.Text.Json;

namespace ReluctersteSDK.DataStructures.Json
{
    /// <summary>
    /// 键值对条目读写的内部辅助工具。
    /// 条目统一表示为一个 JSON 对象：<c>{ "key": ..., "value": ... }</c>。
    /// </summary>
    internal static class KeyValueEntryJson
    {
        internal const string KeyProperty = "key";
        internal const string ValueProperty = "value";

        /// <summary>
        /// 读取光标所在处的 entry 对象，取出 key 与 value。
        /// 进入时 reader 必须位于 <see cref="System.Text.Json.JsonTokenType.StartObject"/>，
        /// 返回时 reader 位于该对象的 <see cref="System.Text.Json.JsonTokenType.EndObject"/>。
        /// </summary>
        internal static void ReadEntry<TKey, TValue>(ref Utf8JsonReader reader, JsonSerializerOptions options,
                                                     out TKey key, out TValue value, string targetName)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"{targetName} 的条目需要以 JSON 对象表示，实际令牌为 {reader.TokenType}。");

            key = default!;
            value = default!;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("期望属性名令牌，实际为 " + reader.TokenType + "。");

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case KeyProperty:
                    case "Key":
                        key = JsonSerializer.Deserialize<TKey>(ref reader, options)!;
                        break;
                    case ValueProperty:
                    case "Value":
                        value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        /// <summary>写出一个 entry 对象</summary>
        internal static void WriteEntry<TKey, TValue>(Utf8JsonWriter writer, TKey key, TValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(KeyProperty);
            JsonSerializer.Serialize(writer, key, typeof(TKey), options);

            writer.WritePropertyName(ValueProperty);
            JsonSerializer.Serialize(writer, value, typeof(TValue), options);

            writer.WriteEndObject();
        }
    }
}
