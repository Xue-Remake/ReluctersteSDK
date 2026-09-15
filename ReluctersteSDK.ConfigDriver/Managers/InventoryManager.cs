using ReluctersteSDK.ConfigDriver.Interfaces;
using ReluctersteSDK.SimpleSqliteORM;

namespace ReluctersteSDK.ConfigDriver.Managers
{
    /// <summary>
    /// 背包/库存管理工具类。
    /// </summary>
    public class InventoryManager
    {
        #region 保存方法 (Entity 转换为 Data 后存入DB)
        /// <summary>保存单个背包/库存实体。</summary>
        public static async Task<bool> TrySaveAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, TInventoryEntity entity)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entity);
            var data = entity.ToData();
            return await database.InsertAsync(data) > 0;
        }
        /// <summary>批量保存背包/库存实体（可变参数）。</summary>
        public static Task<bool> TrySaveAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, params TInventoryEntity[] entities)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
            => TrySaveAsync<TInventoryEntity, TInventoryData>(database, (IEnumerable<TInventoryEntity>)entities);
        /// <summary>批量保存背包/库存实体（集合参数）。</summary>
        public static async Task<bool> TrySaveAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, IEnumerable<TInventoryEntity> entities)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entities);
            foreach (var entity in entities)
            {
                ArgumentNullException.ThrowIfNull(entity);
                var data = entity.ToData();
                if (await database.InsertAsync(data) <= 0)
                    return false;
            }
            return true;
        }
        #endregion
        #region 保存或修改 (Upsert)
        /// <summary>保存或修改单个背包/库存实体（基于主键 Upsert）。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, TInventoryEntity entity)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entity);
            var data = entity.ToData();
            return await database.UpsertAsync(data) > 0;
        }
        /// <summary>批量保存或修改背包/库存实体（可变参数）。</summary>
        public static Task<bool> TrySaveOrUpdateAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, params TInventoryEntity[] entities)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
            => TrySaveOrUpdateAsync<TInventoryEntity, TInventoryData>(database, (IEnumerable<TInventoryEntity>)entities);
        /// <summary>批量保存或修改背包/库存实体（集合参数）。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TInventoryEntity, TInventoryData>(SqliteDataBase database, IEnumerable<TInventoryEntity> entities)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(entities);
            foreach (var entity in entities)
            {
                ArgumentNullException.ThrowIfNull(entity);
                var data = entity.ToData();
                if (await database.UpsertAsync(data) <= 0)
                    return false;
            }
            return true;
        }
        #endregion
        #region 读取方法 (从 DB 查出 Data，并转换为 Entity)
        /// <summary>按主键读取单个背包/库存实体。</summary>
        public static async Task<TInventoryEntity?> GetByIdAsync<TInventoryEntity, TInventoryData>(
            SqliteDataBase database,
            object id)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(id);
            var data = await database.GetByKeyAsync<TInventoryData>(id);
            if (data is null)
                return null;
            return data.ToEntity();
        }
        /// <summary>批量读取背包/库存实体（可变参数）。</summary>
        public static Task<List<TInventoryEntity>> GetByIdsAsync<TInventoryEntity, TInventoryData>(
            SqliteDataBase database,
            params object?[] ids)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>, new()
            => GetByIdsAsync<TInventoryEntity, TInventoryData>(database, (IEnumerable<object?>)ids);
        /// <summary>批量读取背包/库存实体（集合参数）。</summary>
        public static async Task<List<TInventoryEntity>> GetByIdsAsync<TInventoryEntity, TInventoryData>(
            SqliteDataBase database,
            IEnumerable<object?> ids)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(ids);
            var result = new List<TInventoryEntity>();
            foreach (var id in ids)
            {
                if (id is null) continue;
                var data = await database.GetByKeyAsync<TInventoryData>(id);
                if (data is not null)
                {
                    result.Add(data.ToEntity());
                }
            }
            return result;
        }
        /// <summary>按条件查询背包/库存实体列表。</summary>
        public static async Task<List<TInventoryEntity>> QueryAsync<TInventoryEntity, TInventoryData>(
            SqliteDataBase database,
            string? where = null,
            object? parameters = null,
            string? orderBy = null,
            int? limit = null)
            where TInventoryEntity : class, IInventoryEntity<TInventoryEntity, TInventoryData>
            where TInventoryData : class, IInventoryData<TInventoryEntity>, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            // 1. 查询数据库获取 Data 层数据
            var dataList = await database.QueryAsync<TInventoryData>(where, parameters, orderBy, limit);
            // 2. 将 Data 逐一转换为 Entity 层对象
            var resultList = new List<TInventoryEntity>(dataList.Count);
            foreach (var data in dataList)
            {
                if (data is not null)
                {
                    resultList.Add(data.ToEntity());
                }
            }
            return resultList;
        }
        #endregion
    }
}
