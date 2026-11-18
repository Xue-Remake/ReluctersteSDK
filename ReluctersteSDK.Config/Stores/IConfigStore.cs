using ReluctersteSDK.Config.Interfaces;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 配置存储契约。把"怎么存"从"存什么"里剥离出来，
    /// 使内存、JSON 文件、SQLite 三种后端可以互换，也便于单元测试。
    /// </summary>
    /// <typeparam name="T">配置类型。</typeparam>
    public interface IConfigStore<T> where T : class, IConfig, new()
    {
        /// <summary>
        /// 按主键读取单条配置。
        /// </summary>
        /// <param name="keys">主键列；单主键传一个元素，复合主键按顺序传多列。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>找到时返回配置实例，否则返回 null。</returns>
        Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件查询配置列表。
        /// </summary>
        /// <param name="query">查询条件；为 null 表示读取全部。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>匹配的配置列表，未命中时为空列表。</returns>
        Task<IReadOnlyList<T>> QueryAsync(ConfigQuery<T>? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量保存配置。
        /// </summary>
        /// <param name="items">要保存的配置集合。</param>
        /// <param name="mode">插入或覆盖。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>保存结果；失败时携带异常与实际写入条数，不静默吞掉异常。</returns>
        Task<ConfigSaveResult> SaveAsync(
            IReadOnlyList<T> items,
            SaveMode mode,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// <see cref="IConfigStore{T}"/> 的便捷重载。
    /// </summary>
    public static class ConfigStoreExtensions
    {
        /// <summary>按单主键读取。</summary>
        public static Task<T?> GetAsync<T>(
            this IConfigStore<T> store,
            object key,
            CancellationToken cancellationToken = default)
            where T : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(key);
            return store.GetAsync(new object?[] { key }, cancellationToken);
        }

        /// <summary>批量保存配置，默认使用覆盖模式。</summary>
        public static Task<ConfigSaveResult> SaveAsync<T>(
            this IConfigStore<T> store,
            IReadOnlyList<T> items,
            SaveMode mode = SaveMode.Upsert,
            CancellationToken cancellationToken = default)
            where T : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(items);
            return store.SaveAsync(items, mode, cancellationToken);
        }

        /// <summary>保存单条配置。</summary>
        public static Task<ConfigSaveResult> SaveAsync<T>(
            this IConfigStore<T> store,
            T item,
            SaveMode mode = SaveMode.Upsert,
            CancellationToken cancellationToken = default)
            where T : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(item);
            return store.SaveAsync(new[] { item }, mode, cancellationToken);
        }
    }
}
