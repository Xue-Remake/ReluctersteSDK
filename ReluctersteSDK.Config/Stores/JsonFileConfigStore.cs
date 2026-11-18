using System.Text.Json;
using ReluctersteSDK.Config.Codecs;
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Paths;
using ReluctersteSDK.Paths.Tools;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// JSON 文件配置存储：整个配置集序列化成一个数组文件。
    /// 写入采用"临时文件 + 覆盖移动"，避免进程中途退出留下截断的配置文件。
    /// </summary>
    /// <remarks>
    /// 两种构造方式：
    /// <list type="bullet">
    /// <item>传 <see cref="JsonSerializerOptions"/>：走默认反射元数据，简单直接；</item>
    /// <item>传 <see cref="IConfigCodec{T}"/>：走源生成元数据，不反射类型成员，适合裁剪/AOT。</item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">配置类型。</typeparam>
    public sealed class JsonFileConfigStore<T> : IConfigStore<T> where T : class, IConfig, new()
    {
        private readonly string _path;
        private readonly Func<T, object?>? _keySelector;
        private readonly IConfigCodec<T>? _codec;
        private readonly JsonSerializerOptions _options;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private Dictionary<string, T>? _items;

        /// <summary>
        /// 创建 JSON 文件存储（默认反射元数据）。
        /// </summary>
        /// <param name="filePath">配置文件路径。</param>
        /// <param name="keySelector">从配置实例取出主键。</param>
        /// <param name="options">序列化选项；为 null 时使用缩进输出。</param>
        /// <exception cref="ArgumentException">路径无法解析时抛出。</exception>
        public JsonFileConfigStore(
            FdPath filePath,
            Func<T, object?> keySelector,
            JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _options = options ?? new JsonSerializerOptions { WriteIndented = true };
            _path = ResolvePath(filePath);
        }

        /// <summary>
        /// 创建 JSON 文件存储（显式编解码器）。
        /// </summary>
        /// <param name="filePath">配置文件路径。</param>
        /// <param name="codec">编解码器；主键与读写格式都由它决定。</param>
        /// <exception cref="ArgumentException">路径无法解析时抛出。</exception>
        public JsonFileConfigStore(FdPath filePath, IConfigCodec<T> codec)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _options = new JsonSerializerOptions();
            _path = ResolvePath(filePath);
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(keys);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Dictionary<string, T> items = await LoadAsync(cancellationToken).ConfigureAwait(false);
                return items.TryGetValue(ConfigKey.From(keys), out T? value) ? value : null;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> QueryAsync(
            ConfigQuery<T>? query = null,
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Dictionary<string, T> items = await LoadAsync(cancellationToken).ConfigureAwait(false);

                IEnumerable<T> source = items.Values;
                if (query?.Predicate is not null)
                    source = source.Where(query.Predicate);
                if (query?.Limit is int limit)
                    source = source.Take(limit);

                return source.ToList();
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task<ConfigSaveResult> SaveAsync(
            IReadOnlyList<T> items,
            SaveMode mode,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0)
                return ConfigSaveResult.Ok(0);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Dictionary<string, T> map = await LoadAsync(cancellationToken).ConfigureAwait(false);

                // 先整体校验，再统一落盘，避免部分写入。
                if (mode == SaveMode.Insert)
                {
                    foreach (T item in items)
                    {
                        string key = KeyOf(item);
                        if (map.ContainsKey(key))
                            return ConfigSaveResult.Fail(new InvalidOperationException($"配置项已存在：{key}"));
                    }
                }

                foreach (T item in items)
                {
                    map[KeyOf(item)] = item;
                }

                await WriteAsync(map.Values.ToList(), cancellationToken).ConfigureAwait(false);
                return ConfigSaveResult.Ok(items.Count);
            }
            catch (Exception ex)
            {
                return ConfigSaveResult.Fail(ex);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string ResolvePath(FdPath filePath)
        {
            return PathAnalyzer.Analysis(filePath)?.PathStr
                ?? throw new ArgumentException("无法解析配置文件路径。", nameof(filePath));
        }

        private string KeyOf(T item)
        {
            object? key = _codec is not null ? _codec.GetKey(item) : _keySelector!(item);
            return ConfigKey.From(key);
        }

        private async Task<Dictionary<string, T>> LoadAsync(CancellationToken cancellationToken)
        {
            if (_items is not null)
                return _items;

            var map = new Dictionary<string, T>(StringComparer.Ordinal);
            if (File.Exists(_path))
            {
                string json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);

                if (_codec is null)
                {
                    List<T>? list = JsonSerializer.Deserialize<List<T>>(json, _options);
                    if (list is not null)
                    {
                        foreach (T item in list)
                        {
                            map[KeyOf(item)] = item;
                        }
                    }
                }
                else
                {
                    using JsonDocument document = JsonDocument.Parse(json);
                    foreach (JsonElement element in document.RootElement.EnumerateArray())
                    {
                        T item = _codec.Read(element);
                        map[KeyOf(item)] = item;
                    }
                }
            }

            _items = map;
            return map;
        }

        private async Task WriteAsync(List<T> values, CancellationToken cancellationToken)
        {
            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string tempPath = _path + ".tmp";

            if (_codec is null)
            {
                string json = JsonSerializer.Serialize(values, _options);
                await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await using var stream = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                writer.WriteStartArray();
                foreach (T item in values)
                {
                    _codec.Write(writer, item);
                }
                writer.WriteEndArray();
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
    }
}
