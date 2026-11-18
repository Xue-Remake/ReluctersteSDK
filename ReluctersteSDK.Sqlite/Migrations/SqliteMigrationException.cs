namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>迁移方向。</summary>
    public enum MigrationDirection
    {
        /// <summary>升级（<c>UpAsync</c>）。</summary>
        Up,

        /// <summary>回滚（<c>DownAsync</c>）。</summary>
        Down
    }

    /// <summary>
    /// 迁移执行失败。原始异常保留在 <see cref="Exception.InnerException"/> 中。
    /// </summary>
    public sealed class SqliteMigrationException : Exception
    {
        /// <summary>
        /// 创建迁移异常。
        /// </summary>
        /// <param name="version">失败的迁移版本。</param>
        /// <param name="description">迁移说明。</param>
        /// <param name="direction">失败发生的方向。</param>
        /// <param name="innerException">原始异常。</param>
        public SqliteMigrationException(
            long version,
            string description,
            MigrationDirection direction,
            Exception innerException)
            : base(
                $"Migration {version} ({description}) failed during {direction}.",
                innerException)
        {
            Version = version;
            Description = description;
            Direction = direction;
        }

        /// <summary>获取失败的迁移版本。</summary>
        public long Version { get; }

        /// <summary>获取失败的迁移说明。</summary>
        public string Description { get; }

        /// <summary>获取失败发生的方向。</summary>
        public MigrationDirection Direction { get; }
    }
}
