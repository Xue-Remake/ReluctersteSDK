using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ReluctersteSDK.Config.Interfaces;

namespace ReluctersteSDK.Config.Codecs
{
    /// <summary>
    /// 基于 <see cref="JsonTypeInfo{T}"/> 的配置编解码器。
    /// 元数据可以来自源生成上下文（<see cref="FromContext"/>），也可以手工构造，
    /// 两条路径都不在运行时反射类型成员。
    /// </summary>
    /// <typeparam name="T">配置类型。</typeparam>
    public sealed class JsonConfigCodec<T> : IConfigCodec<T> where T : class, IConfig
    {
        private readonly JsonTypeInfo<T> _typeInfo;
        private readonly Func<T, object?> _keySelector;

        /// <summary>
        /// 创建编解码器。
        /// </summary>
        /// <param name="typeInfo">该类型的 JSON 元数据。</param>
        /// <param name="keySelector">主键选择器。</param>
        /// <param name="typeName">类型名；为 null 时使用 <see cref="Type.FullName"/>。</param>
        public JsonConfigCodec(
            JsonTypeInfo<T> typeInfo,
            Func<T, object?> keySelector,
            string? typeName = null)
        {
            _typeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            TypeName = typeName ?? typeof(T).FullName ?? typeof(T).Name;
        }

        /// <summary>
        /// 从源生成上下文创建编解码器。
        /// </summary>
        /// <param name="context">包含 <typeparamref name="T"/> 元数据的 <see cref="JsonSerializerContext"/>。</param>
        /// <param name="keySelector">主键选择器。</param>
        /// <param name="typeName">类型名；为 null 时使用 <see cref="Type.FullName"/>。</param>
        /// <exception cref="InvalidOperationException">上下文中没有该类型的元数据时抛出。</exception>
        public static JsonConfigCodec<T> FromContext(
            JsonSerializerContext context,
            Func<T, object?> keySelector,
            string? typeName = null)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> typeInfo)
            {
                throw new InvalidOperationException(
                    $"JsonSerializerContext 未包含 {typeof(T).FullName} 的元数据；" +
                    $"请在该上下文的 [JsonSerializable] 中声明它。");
            }

            return new JsonConfigCodec<T>(typeInfo, keySelector, typeName);
        }

        /// <inheritdoc />
        public string TypeName { get; }

        /// <inheritdoc />
        public object? GetKey(T value) => _keySelector(value);

        /// <inheritdoc />
        public void Write(Utf8JsonWriter writer, T value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);
            JsonSerializer.Serialize(writer, value, _typeInfo);
        }

        /// <inheritdoc />
        public T Read(JsonElement element)
        {
            return element.Deserialize(_typeInfo)
                ?? throw new JsonException($"配置 {typeof(T).FullName} 反序列化结果为 null。");
        }
    }
}
