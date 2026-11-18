using System.Text.Json;
using System.Text.Json.Serialization;
using ReluctersteSDK.DataStructures.Structures;

namespace ReluctersteSDK.DataStructures.Json
{
    /// <summary>
    /// <see cref="ListDictionary{TKey, TValue}"/> 的 JSON 转换器。
    /// 序列化形式为一个条目数组，每个条目包含 key 与 value 两个字段；由于底层为有序列表，
    /// 反序列化的顺序与 JSON 中条目出现的顺序完全一致。
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public sealed class ListDictionaryConverter<TKey, TValue> : JsonConverter<ListDictionary<TKey, TValue>>
    {
        private const string TargetName = "ListDictionary";

        /// <summary>反序列化一个列表字典</summary>
        /// <exception cref="JsonException">JSON 结构非法，或 key 为 null 时抛出</exception>
        public override ListDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"{TargetName}<{typeof(TKey).Name}, {typeof(TValue).Name}> 需要以 JSON 数组表示，实际令牌为 {reader.TokenType}。");

            var dictionary = new ListDictionary<TKey, TValue>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;

                KeyValueEntryJson.ReadEntry<TKey, TValue>(ref reader, options, out var key, out var value, TargetName);

                if (key is null) throw new JsonException($"{TargetName} 的 key 不能为 null。");

                dictionary.SetValue(key, value!);
            }

            return dictionary;
        }

        /// <summary>序列化一个列表字典</summary>
        public override void Write(Utf8JsonWriter writer, ListDictionary<TKey, TValue> value, JsonSerializerOptions options)
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
    /// 用于为任意封闭的 <see cref="ListDictionary{TKey, TValue}"/> 创建 <see cref="ListDictionaryConverter{TKey, TValue}"/> 的工厂。
    /// </summary>
    public sealed class ListDictionaryConverterFactory : JsonConverterFactory
    {
        /// <summary>判断是否为可处理的 <see cref="ListDictionary{TKey, TValue}"/></summary>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                   && !typeToConvert.IsGenericTypeDefinition
                   && !typeToConvert.ContainsGenericParameters
                   && typeToConvert.GetGenericTypeDefinition() == typeof(ListDictionary<,>);
        }

        /// <summary>创建对应的封闭类型转换器</summary>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var typeArguments = typeToConvert.GetGenericArguments();
            var converterType = typeof(ListDictionaryConverter<,>).MakeGenericType(typeArguments);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
