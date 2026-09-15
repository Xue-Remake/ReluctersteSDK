using ReluctersteSDK.ConfigDriver.Interfaces;
using ReluctersteSDK.SimpleSqliteORM;

namespace ReluctersteSDK.ConfigDriver.Managers
{
    /// <summary>
    /// 物品管理静态工具类。
    /// </summary>
    public static class ItemManager
    {
        #region 保存方法
        /// <summary>保存单条物品（按实例参数）。返回是否写入成功（受影响行数 &gt; 0）；主键冲突等异常照常抛出。</summary>
        public static async Task<bool> TrySaveAsync<TEntity, TData>(SqliteDataBase database, TEntity entity)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entity);

            return await database.InsertAsync(entity.ToData()) > 0;
        }

        /// <summary>批量保存物品（按数组参数，支持可变参数调用，如 <c>TrySaveAsync(db, a, b, c)</c>）。全部成功才返回 <c>true</c>。</summary>
        public static Task<bool> TrySaveAsync<TEntity, TData>(SqliteDataBase database, params TEntity[] entities)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
            => TrySaveAsync<TEntity, TData>(database, (IEnumerable<TEntity>)entities);

        /// <summary>批量保存物品（按列表 / 任意集合参数）。全部成功才返回 <c>true</c>。</summary>
        public static async Task<bool> TrySaveAsync<TEntity, TData>(SqliteDataBase database, IEnumerable<TEntity> entities)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entities);

            foreach (var entity in entities)
            {
                ArgumentNullException.ThrowIfNull(entity);
                if (await database.InsertAsync(entity.ToData()) <= 0)
                    return false;
            }
            return true;
        }
        #endregion

        #region 保存或修改
        /// <summary>保存或修改单条物品（按实例参数，基于主键 Upsert）。返回是否成功；异常照常抛出。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TEntity, TData>(SqliteDataBase database, TEntity entity)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entity);

            return await database.UpsertAsync(entity.ToData()) > 0;
        }

        /// <summary>批量保存或修改物品（按数组参数，支持可变参数调用）。全部成功才返回 <c>true</c>。</summary>
        public static Task<bool> TrySaveOrUpdateAsync<TEntity, TData>(SqliteDataBase database, params TEntity[] entities)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
            => TrySaveOrUpdateAsync<TEntity, TData>(database, (IEnumerable<TEntity>)entities);

        /// <summary>批量保存或修改物品（按列表 / 任意集合参数）。全部成功才返回 <c>true</c>。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TEntity, TData>(SqliteDataBase database, IEnumerable<TEntity> entities)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entities);

            foreach (var entity in entities)
            {
                ArgumentNullException.ThrowIfNull(entity);
                if (await database.UpsertAsync(entity.ToData()) <= 0)
                    return false;
            }
            return true;
        }
        #endregion

        #region 读取方法
        /// <summary>
        /// 按主键 id 读取单条物品。主键列由 ORM 根据 [DbKey] 特性自动解析，本类不关心列名；
        /// 未找到时返回 <c>null</c>。
        /// </summary>
        public static async Task<TEntity?> GetByIdAsync<TEntity, TData>(SqliteDataBase database, object id)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(id);

            var data = await database.GetByKeyAsync<TData>(id);
            ArgumentNullException.ThrowIfNull(data);
            return data.ToEntity();
        }

        /// <summary>
        /// 按 where 条件读取物品列表（按 SQL 语句参数）。
        /// 列名需由调用方按自己的表结构书写，值一律走参数绑定。
        /// </summary>
        /// <param name="database">数据库实例。</param>
        /// <param name="where">WHERE 子句，可带可不带 WHERE 关键字。</param>
        /// <param name="parameters">查询参数（匿名对象 / 字典 / KeyValuePair 集合等）。</param>
        /// <param name="orderBy">ORDER BY 子句（不含 ORDER BY 关键字）。</param>
        /// <param name="limit">限制返回行数。</param>
        public static async Task<List<TEntity>> QueryAsync<TEntity, TData>(
            SqliteDataBase database,
            string? where = null,
            object? parameters = null,
            string? orderBy = null,
            int? limit = null)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);

            var datas = await database.QueryAsync<TData>(where, parameters, orderBy, limit);
            List<TEntity> entities = new();
            foreach (var data in datas)
            {
                entities.Add(data.ToEntity());
            }
            return entities;
        }

        /// <summary>
        /// 批量读取物品（按 <c>params</c> 可变参数，如 <c>GetByIdsAsync(db, 1, 2, 3)</c>）。
        /// 逐个按主键读取，未找到的 id 自动跳过。
        /// </summary>
        public static Task<List<TEntity>> GetByIdsAsync<TEntity, TData>(SqliteDataBase database, params object?[] ids)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
            => GetByIdsAsync<TEntity, TData>(database, (IEnumerable<object?>)ids);

        /// <summary>
        /// 批量读取物品（按 id 数组 / 集合参数，如 <c>GetByIdsAsync(db, idArray)</c>）。
        /// 逐个按主键读取，未找到的 id 自动跳过。
        /// </summary>
        public static async Task<List<TEntity>> GetByIdsAsync<TEntity, TData>(SqliteDataBase database, IEnumerable<object?> ids)
            where TEntity : class, IItemEntity<TEntity, TData>
            where TData : class, IItemData<TEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(ids);

            var result = new List<TEntity>();
            foreach (var id in ids)
            {
                if (id is null)
                    continue;

                var data = await database.GetByKeyAsync<TData>(id);
                if (data is not null)
                    result.Add(data.ToEntity());
            }
            return result;
        }
        #endregion
    }
}
