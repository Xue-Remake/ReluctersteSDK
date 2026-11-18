using System.Text.Json;

namespace ReluctersteSDK.Registry.Json
{
    /// <summary>
    /// 注册表类型转换器的注册入口。
    /// 支持将 <see cref="ValueRegistry{TKey, TValue}"/>、<see cref="CompositeValueRegistry{TValue}"/>
    /// 与 <see cref="CompositeKey"/> 的转换器注册到 <see cref="JsonSerializerOptions"/>。
    /// </summary>
    public static class RegistryJson
    {
        private static readonly object SyncRoot = new();
        private static volatile JsonSerializerOptions? _default;

        /// <summary>
        /// 已注册全部注册表转换器的默认选项（线程安全懒加载）。
        /// </summary>
        /// <remarks>
        /// 该实例是全局共享的：首次用于序列化后会被 System.Text.Json 冻结，
        /// 之后修改 <see cref="JsonSerializerOptions.Converters"/> 会抛异常。
        /// 需要自定义选项时请改用 <see cref="CreateOptions"/> 或 <see cref="UseRegistry"/>。
        /// </remarks>
        public static JsonSerializerOptions Default
        {
            get
            {
                if (_default is not null) return _default;

                lock (SyncRoot)
                {
                    return _default ??= CreateOptions();
                }
            }
        }

        /// <summary>创建一份新的、已注册注册表转换器的选项</summary>
        /// <param name="writeIndented">是否缩进输出，默认 true</param>
        public static JsonSerializerOptions CreateOptions(bool writeIndented = true)
        {
            return RegisterTo(new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                WriteIndented = writeIndented
            });
        }

        /// <summary>
        /// 在既有选项上注册注册表转换器（幂等，重复调用不会重复添加）。
        /// </summary>
        /// <param name="options">要扩展的选项</param>
        /// <returns>同一个 <paramref name="options"/> 实例，便于链式调用</returns>
        public static JsonSerializerOptions UseRegistry(this JsonSerializerOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            return RegisterTo(options);
        }

        private static JsonSerializerOptions RegisterTo(JsonSerializerOptions options)
        {
            AddIfMissing<CompositeKeyConverter>(options);
            AddIfMissing<ValueRegistryConverterFactory>(options);
            AddIfMissing<CompositeValueRegistryConverterFactory>(options);
            return options;
        }

        private static void AddIfMissing<TConverter>(JsonSerializerOptions options) where TConverter : System.Text.Json.Serialization.JsonConverter, new()
        {
            foreach (var existing in options.Converters)
            {
                if (existing is TConverter) return;
            }
            options.Converters.Add(new TConverter());
        }
    }
}
