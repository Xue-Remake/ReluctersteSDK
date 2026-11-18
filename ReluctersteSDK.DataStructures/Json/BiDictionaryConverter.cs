using System.Text.Json;
using System.Text.Json.Serialization;
using ReluctersteSDK.DataStructures.Structures;

namespace ReluctersteSDK.DataStructures.Json
{
    /// <summary>
    /// <see cref="BiDictionary{TKey, TValue}"/> 的 JSON 转换器。
    /// 序列化形式为一个条目数组，每个条目包含 key 与 value 两个字段，保持正向字典的枚举顺序。
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public sealed class BiDictionaryConverter<TKey, TValue> : JsonConverter<BiDictionary<TKey, TValue>>
    {
        private const string TargetName = "BiDictionary";

        /// <summary>反序列化一个双向字典</summary>
        /// <exception cref="JsonException">JSON 结构非法，或键/值为 null 时抛出（本结构的键与值均不可为 null）</exception>
        public override BiDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"{TargetName}<{typeof(TKey).Name}, {typeof(TValue).Name}> 需要以 JSON 数组表示，实际令牌为 {reader.TokenType}。");

            var dictionary = new BiDictionary<TKey, TValue>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;

                KeyValueEntryJson.ReadEntry<TKey, TValue>(ref reader, options, out var key, out var value, TargetName);

                if (key is null) throw new JsonException($"{TargetName} 的 key 不能为 null。");
                if (value is null) throw new JsonException($"{TargetName} 的 value 不能为 null（本结构要求值唯一，因此也不可为空）。");

                dictionary.AddOrUpdate(key, value);
            }

            return dictionary;
        }

        /// <summary>序列化一个双向字典</summary>
        public override void Write(Utf8JsonWriter writer, BiDictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var pair in value)
            {
                KeyValueEntryJson.WriteEntry(writer, pair.Key, pair.Value, options);
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// 用于为任意封闭的 <see cref="BiDictionary{TKey, TValue}"/> 创建 <see cref="BiDictionaryConverter{TKey, TValue}"/> 的工厂。
    /// </summary>
    public sealed class BiDictionaryConverterFactory : JsonConverterFactory
    {
        /// <summary>判断是否为可处理的 <see cref="BiDictionary{TKey, TValue}"/></summary>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                   && !typeToConvert.IsGenericTypeDefinition
                   && !typeToConvert.ContainsGenericParameters
                   && typeToConvert.GetGenericTypeDefinition() == typeof(BiDictionary<,>);
        }

        /// <summary>创建对应的封闭类型转换器</summary>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var typeArguments = typeToConvert.GetGenericArguments();
            var converterType = typeof(BiDictionaryConverter<,>).MakeGenericType(typeArguments);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
