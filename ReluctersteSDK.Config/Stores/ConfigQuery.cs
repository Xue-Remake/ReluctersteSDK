namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 配置查询条件。用谓词而不是 SQL 字符串，
    /// 这样内存、JSON、SQLite 三种存储可以共用同一套查询语义。
    /// </summary>
    /// <typeparam name="T">配置类型。</typeparam>
    public sealed class ConfigQuery<T>
    {
        /// <summary>筛选谓词；为 null 时表示不过滤。</summary>
        public Func<T, bool>? Predicate { get; init; }

        /// <summary>最多返回的条数；为 null 时表示不限。</summary>
        public int? Limit { get; init; }
    }
}
