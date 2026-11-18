using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Sqlite;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// SQLite 配置存储，基于 <see cref="SqliteDataBase"/> 的特性映射。
    /// </summary>
    /// <remarks>
    /// 首次访问会调用 <see cref="SqliteDataBase.EnsureTableAsync{T}"/> 建表，
    /// 不再依赖调用方自己记得先建表。
    /// 批量保存逐条提交：SQLite ORM 目前没有公开事务 API，
    /// 因此失败时请通过 <see cref="ConfigSaveResult.Written"/> 判断已写入条数。
    /// </remarks>
    /// <typeparam name="T">配置类型，需带 <c>[DbTable]</c>/<c>[DbKey]</c> 等映射特性。</typeparam>
    public sealed class SqliteConfigStore<T> : IConfigStore<T> where T : class, IConfig, new()
    {
        private readonly SqliteDataBase _database;
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private bool _tableEnsured;

        /// <summary>
        /// 创建 SQLite 配置存储。
        /// </summary>
        /// <param name="database">数据库实例；生命周期由调用方管理。</param>
        public SqliteConfigStore(SqliteDataBase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(keys);

            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await _database.GetByKeyAsync<T>(keys.ToArray()).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> QueryAsync(
            ConfigQuery<T>? query = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            List<T> all = await _database.GetAllAsync<T>().ConfigureAwait(false);

            IEnumerable<T> source = all;
            if (query?.Predicate is not null)
                source = source.Where(query.Predicate);
            if (query?.Limit is int limit)
                source = source.Take(limit);

            return source.ToList();
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

            int written = 0;
            try
            {
                await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

                foreach (T item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (mode == SaveMode.Insert)
                        await _database.InsertAsync(item).ConfigureAwait(false);
                    else
                        await _database.UpsertAsync(item).ConfigureAwait(false);

                    written++;
                }

                return ConfigSaveResult.Ok(written);
            }
            catch (Exception ex)
            {
                return ConfigSaveResult.Fail(ex, written);
            }
        }

        private async Task EnsureTableAsync(CancellationToken cancellationToken)
        {
            if (_tableEnsured)
                return;

            await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_tableEnsured)
                    return;

                await _database.EnsureTableAsync<T>().ConfigureAwait(false);
                _tableEnsured = true;
            }
            finally
            {
                _initGate.Release();
            }
        }
    }
}
