using ReluctersteSDK.Config.Interfaces;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 把"业务实体 ↔ 存储数据"的转换包在任意 <see cref="IConfigStore{T}"/> 外面。
    /// 取代原先 <c>ItemManager</c> 与 <c>InventoryManager</c> 两套几乎逐行重复的静态方法。
    /// </summary>
    /// <typeparam name="TEntity">业务实体类型。</typeparam>
    /// <typeparam name="TData">持久化数据类型。</typeparam>
    public sealed class EntityConfigStore<TEntity, TData>
        where TEntity : class, IEntity<TEntity, TData>
        where TData : class, IData<TEntity>, IConfig, new()
    {
        private readonly IConfigStore<TData> _store;

        /// <summary>
        /// 创建实体存储适配器。
        /// </summary>
        /// <param name="store">底层配置存储（SQLite / JSON / 内存均可）。</param>
        public EntityConfigStore(IConfigStore<TData> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>按主键读取实体。</summary>
        /// <param name="keys">主键列。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<TEntity?> GetAsync(
            IReadOnlyList<object?> keys,
            CancellationToken cancellationToken = default)
        {
            TData? data = await _store.GetAsync(keys, cancellationToken).ConfigureAwait(false);
            return data is null ? null : data.ToEntity();
        }

        /// <summary>按条件查询实体列表。</summary>
        /// <param name="dataQuery">针对持久化数据的查询条件。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<IReadOnlyList<TEntity>> QueryAsync(
            ConfigQuery<TData>? dataQuery = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TData> data = await _store.QueryAsync(dataQuery, cancellationToken).ConfigureAwait(false);

            var result = new List<TEntity>(data.Count);
            foreach (TData item in data)
            {
                result.Add(item.ToEntity());
            }
            return result;
        }

        /// <summary>批量保存实体。</summary>
        /// <param name="entities">要保存的实体集合。</param>
        /// <param name="mode">插入或覆盖。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<ConfigSaveResult> SaveAsync(
            IReadOnlyList<TEntity> entities,
            SaveMode mode,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entities);

            var data = new List<TData>(entities.Count);
            foreach (TEntity entity in entities)
            {
                data.Add(entity.ToData());
            }

            return _store.SaveAsync(data, mode, cancellationToken);
        }

        /// <summary>保存单个实体。</summary>
        /// <param name="entity">要保存的实体。</param>
        /// <param name="mode">插入或覆盖。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<ConfigSaveResult> SaveAsync(
            TEntity entity,
            SaveMode mode = SaveMode.Upsert,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return SaveAsync(new[] { entity }, mode, cancellationToken);
        }
    }
}
