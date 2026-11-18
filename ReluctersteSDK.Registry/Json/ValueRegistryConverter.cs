using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReluctersteSDK.Registry.Json
{
    /// <summary>
    /// <see cref="ValueRegistry{TKey, TValue}"/> 的 JSON 转换器。
    /// 序列化形式为一个条目数组，每个条目包含 key、value 与 enabled 三个字段，
    /// 其中被禁用的条目同样会被写入 JSON，反序列化后自动调用 <c>Disable</c> 还原状态。
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public sealed class ValueRegistryConverter<TKey, TValue> : JsonConverter<ValueRegistry<TKey, TValue>>
    {
        private const string KeyProperty = "key";
        private const string ValueProperty = "value";
        private const string EnabledProperty = "enabled";

        /// <summary>反序列化一个注册表实例</summary>
        /// <exception cref="JsonException">JSON 结构非法或缺少 key 字段时抛出</exception>
        public override ValueRegistry<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"ValueRegistry<{typeof(TKey).Name}, {typeof(TValue).Name}> 需要以 JSON 数组表示，实际令牌为 {reader.TokenType}。");

            var registry = new ValueRegistry<TKey, TValue>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("注册表条目需要以 JSON 对象表示，实际令牌为 " + reader.TokenType + "。");

                TKey key = default!;
                TValue value = default!;
                bool enabled = true;
                bool hasKey = false;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("期望属性名令牌，实际为 " + reader.TokenType + "。");

                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case KeyProperty:
                        case "Key":
                            key = JsonSerializer.Deserialize<TKey>(ref reader, options)!;
                            hasKey = true;
                            break;
                        case ValueProperty:
                        case "Value":
                            value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
                            break;
                        case EnabledProperty:
                        case "Enabled":
                            enabled = reader.TokenType != JsonTokenType.Null && reader.GetBoolean();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                if (!hasKey)
                    throw new JsonException("注册表条目缺少必需的 key 字段。");

                registry.Register(key, value);
                if (!enabled) registry.Disable(key);
            }

            return registry;
        }

        /// <summary>序列化一个注册表实例</summary>
        public override void Write(Utf8JsonWriter writer, ValueRegistry<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var entry in value.GetEntrySnapshot())
            {
                writer.WriteStartObject();

                writer.WritePropertyName(KeyProperty);
                JsonSerializer.Serialize(writer, entry.Key, typeof(TKey), options);

                writer.WritePropertyName(ValueProperty);
                JsonSerializer.Serialize(writer, entry.Value, typeof(TValue), options);

                writer.WritePropertyName(EnabledProperty);
                writer.WriteBooleanValue(entry.Enabled);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// 用于为任意封闭的 <see cref="ValueRegistry{TKey, TValue}"/> 创建 <see cref="ValueRegistryConverter{TKey, TValue}"/> 的工厂。
    /// </summary>
    public sealed class ValueRegistryConverterFactory : JsonConverterFactory
    {
        /// <summary>判断是否为可处理的 <see cref="ValueRegistry{TKey, TValue}"/></summary>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                   && !typeToConvert.IsGenericTypeDefinition
                   && !typeToConvert.ContainsGenericParameters
                   && typeToConvert.GetGenericTypeDefinition() == typeof(ValueRegistry<,>);
        }

        /// <summary>创建对应的封闭类型转换器</summary>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var typeArguments = typeToConvert.GetGenericArguments();
            var converterType = typeof(ValueRegistryConverter<,>).MakeGenericType(typeArguments);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
