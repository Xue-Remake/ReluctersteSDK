namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// 一次数据库结构变更。版本号必须唯一且大于 0，执行顺序按版本号升序。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 继承本类并实现 <see cref="UpAsync"/> 即可；需要支持回滚时再重写
    /// <see cref="DownAsync"/>。每个迁移在独立事务中执行，失败会整体回滚，
    /// 不会写入迁移历史。
    /// </para>
    /// <para>
    /// 简单场景可以用 <see cref="Create"/> 直接由委托构造，省去子类样板代码。
    /// </para>
    /// </remarks>
    public abstract class SqliteMigration
    {
        /// <summary>获取迁移版本号；必须大于 0 且在同一次迁移中唯一。</summary>
        public abstract long Version { get; }

        /// <summary>获取迁移说明，写入历史表并用于错误信息。</summary>
        public abstract string Description { get; }

        /// <summary>
        /// 执行升级。
        /// </summary>
        /// <param name="context">迁移上下文，提供连接、事务与 DDL 助手。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public abstract Task UpAsync(
            SqliteMigrationContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行回滚。默认抛出 <see cref="NotSupportedException"/>。
        /// </summary>
        /// <param name="context">迁移上下文。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public virtual Task DownAsync(
            SqliteMigrationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                $"Migration {Version} ({Description}) does not define DownAsync.");
        }

        /// <summary>
        /// 使用委托创建一个迁移。
        /// </summary>
        /// <param name="version">版本号，必须大于 0。</param>
        /// <param name="description">迁移说明。</param>
        /// <param name="up">升级委托。</param>
        /// <param name="down">可选的恢复委托；为 null 时该迁移不可回滚。</param>
        public static SqliteMigration Create(
            long version,
            string description,
            Func<SqliteMigrationContext, CancellationToken, Task> up,
            Func<SqliteMigrationContext, CancellationToken, Task>? down = null)
        {
            ArgumentNullException.ThrowIfNull(up);
            return new DelegateSqliteMigration(version, description, up, down);
        }

        /// <summary>委托形式的迁移实现。</summary>
        private sealed class DelegateSqliteMigration : SqliteMigration
        {
            private readonly Func<SqliteMigrationContext, CancellationToken, Task> _up;
            private readonly Func<SqliteMigrationContext, CancellationToken, Task>? _down;

            public DelegateSqliteMigration(
                long version,
                string description,
                Func<SqliteMigrationContext, CancellationToken, Task> up,
                Func<SqliteMigrationContext, CancellationToken, Task>? down)
            {
                Version = version;
                Description = description;
                _up = up;
                _down = down;
            }

            public override long Version { get; }

            public override string Description { get; }

            public override Task UpAsync(
                SqliteMigrationContext context,
                CancellationToken cancellationToken = default)
                => _up(context, cancellationToken);

            public override Task DownAsync(
                SqliteMigrationContext context,
                CancellationToken cancellationToken = default)
            {
                if (_down == null)
                    return base.DownAsync(context, cancellationToken);

                return _down(context, cancellationToken);
            }
        }
    }
}
