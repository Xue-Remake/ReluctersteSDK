using ReluctersteSDK.ConfigDriver.Interfaces;
using ReluctersteSDK.PathHelper;
using ReluctersteSDK.PathHelper.Tools;
using ReluctersteSDK.SimpleSqliteORM;
using System.Text.Json;

namespace ReluctersteSDK.ConfigDriver.Managers
{
    /// <summary>
    /// 配置管理静态工具类。
    /// 直接操作实现 <see cref="IConfig"/> 的配置类，该类本身就是持久化数据模型。
    /// </summary>
    public static class ConfigManager
    {
        #region 保存方法
        /// <summary>保存单个配置对象。</summary>
        public static async Task<bool> TrySaveAsync<TConfig>(SqliteDataBase database, TConfig config)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(config);
            return await database.InsertAsync(config) > 0;
        }
        /// <summary>批量保存配置（可变参数）。全部成功才返回 true。</summary>
        public static Task<bool> TrySaveAsync<TConfig>(SqliteDataBase database, params TConfig[] configs)
            where TConfig : class, IConfig, new()
            => TrySaveAsync<TConfig>(database, (IEnumerable<TConfig>)configs);
        /// <summary>批量保存配置（集合参数）。全部成功才返回 true。</summary>
        public static async Task<bool> TrySaveAsync<TConfig>(SqliteDataBase database, IEnumerable<TConfig> configs)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(configs);
            foreach (var config in configs)
            {
                ArgumentNullException.ThrowIfNull(config);
                if (await database.InsertAsync(config) <= 0)
                    return false;
            }
            return true;
        }
        #endregion
        #region 保存或修改（Upsert）
        /// <summary>保存或修改单个配置（基于主键 Upsert）。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TConfig>(SqliteDataBase database, TConfig config)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(config);
            return await database.UpsertAsync(config) > 0;
        }
        /// <summary>批量保存或修改配置（可变参数）。</summary>
        public static Task<bool> TrySaveOrUpdateAsync<TConfig>(SqliteDataBase database, params TConfig[] configs)
            where TConfig : class, IConfig, new()
            => TrySaveOrUpdateAsync<TConfig>(database, (IEnumerable<TConfig>)configs);
        /// <summary>批量保存或修改配置（集合参数）。</summary>
        public static async Task<bool> TrySaveOrUpdateAsync<TConfig>(SqliteDataBase database, IEnumerable<TConfig> configs)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(configs);
            foreach (var config in configs)
            {
                ArgumentNullException.ThrowIfNull(config);
                if (await database.UpsertAsync(config) <= 0)
                    return false;
            }
            return true;
        }
        #endregion
        #region 读取方法
        /// <summary>按主键读取单个配置。</summary>
        public static async Task<TConfig?> GetByIdAsync<TConfig>(SqliteDataBase database, object id)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(id);
            return await database.GetByKeyAsync<TConfig>(id);
        }
        /// <summary>按可变参数批量读取配置。</summary>
        public static Task<List<TConfig>> GetByIdsAsync<TConfig>(SqliteDataBase database, params object?[] ids)
            where TConfig : class, IConfig, new()
            => GetByIdsAsync<TConfig>(database, (IEnumerable<object?>)ids);
        /// <summary>按 id 集合批量读取配置。</summary>
        public static async Task<List<TConfig>> GetByIdsAsync<TConfig>(SqliteDataBase database, IEnumerable<object?> ids)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(ids);
            var result = new List<TConfig>();
            foreach (var id in ids)
            {
                if (id is null) continue;
                var config = await database.GetByKeyAsync<TConfig>(id);
                if (config is not null)
                    result.Add(config);
            }
            return result;
        }
        /// <summary>按条件查询配置列表。</summary>
        public static async Task<List<TConfig>> QueryAsync<TConfig>(
            SqliteDataBase database,
            string? where = null,
            object? parameters = null,
            string? orderBy = null,
            int? limit = null)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(database);
            return await database.QueryAsync<TConfig>(where, parameters, orderBy, limit);
        }
        #endregion

        #region JSON 序列化保存/读取方法（基于 PathHelper）
        /// <summary>
        /// 将单个配置对象序列化为 JSON 并保存到指定文件。
        /// 文件路径支持 <see cref="FdPath"/>（绝对路径或相对路径），若路径非法或写入失败返回 false。
        /// 如果文件所在目录不存在，将自动创建。
        /// </summary>
        public static async Task<bool> TrySaveJsonAsync<TConfig>(FdPath filePath, TConfig config)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(config);
            var absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return false;
            try
            {
                // 确保目录存在
                string? directory = Path.GetDirectoryName(absPath.PathStr);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var json = JsonSerializer.Serialize(config, typeof(TConfig));
                await File.WriteAllTextAsync(absPath.PathStr, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 将配置对象集合序列化为 JSON 数组并保存到指定文件。
        /// 若路径非法或写入失败返回 false。
        /// </summary>
        public static async Task<bool> TrySaveJsonAsync<TConfig>(FdPath filePath, IEnumerable<TConfig> configs)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(configs);
            var absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return false;
            try
            {
                string? directory = Path.GetDirectoryName(absPath.PathStr);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var json = JsonSerializer.Serialize(configs, typeof(List<TConfig>));
                await File.WriteAllTextAsync(absPath.PathStr, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 从指定 JSON 文件读取并反序列化为单个配置对象。
        /// 文件不存在、路径非法或反序列化失败均返回 null。
        /// </summary>
        public static async Task<TConfig?> GetFromJsonAsync<TConfig>(FdPath filePath)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(filePath);
            var absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return null;
            if (!File.Exists(absPath.PathStr)) return null;
            try
            {
                var json = await File.ReadAllTextAsync(absPath.PathStr);
                return JsonSerializer.Deserialize<TConfig>(json);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 从指定 JSON 文件读取并反序列化为配置对象列表。
        /// 文件不存在、路径非法或反序列化失败均返回 null。
        /// </summary>
        public static async Task<List<TConfig>?> GetListFromJsonAsync<TConfig>(FdPath filePath)
            where TConfig : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(filePath);
            var absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return null;
            if (!File.Exists(absPath.PathStr)) return null;
            try
            {
                var json = await File.ReadAllTextAsync(absPath.PathStr);
                return JsonSerializer.Deserialize<List<TConfig>>(json);
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
