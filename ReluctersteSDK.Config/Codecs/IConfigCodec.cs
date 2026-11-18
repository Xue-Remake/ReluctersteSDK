using System.Text.Json;
using ReluctersteSDK.Config.Interfaces;

namespace ReluctersteSDK.Config.Codecs
{
    /// <summary>
    /// 配置编解码器。把"某个配置类型怎么读写 JSON、主键是什么"显式交给调用方，
    /// 而不是依赖运行时反射去猜。
    /// </summary>
    /// <typeparam name="T">配置类型。</typeparam>
    public interface IConfigCodec<T> where T : class, IConfig
    {
        /// <summary>稳定的类型名，用于诊断与多态配置。</summary>
        string TypeName { get; }

        /// <summary>从配置实例取出主键。</summary>
        object? GetKey(T value);

        /// <summary>把单个配置写成 JSON。</summary>
        void Write(Utf8JsonWriter writer, T value);

        /// <summary>从 JSON 读出一个配置。</summary>
        /// <exception cref="JsonException">数据无法反序列化为 <typeparamref name="T"/> 时抛出。</exception>
        T Read(JsonElement element);
    }
}
