namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// 通过 <c>PRAGMA table_info</c> 读取到的列信息。
    /// </summary>
    public sealed class SqliteColumnInfo
    {
        internal SqliteColumnInfo(
            string name,
            string declaredType,
            bool isNotNull,
            string? defaultValue,
            int primaryKeyOrder)
        {
            Name = name;
            DeclaredType = declaredType;
            IsNotNull = isNotNull;
            DefaultValue = defaultValue;
            PrimaryKeyOrder = primaryKeyOrder;
        }

        /// <summary>获取列名。</summary>
        public string Name { get; }

        /// <summary>获取声明类型（如 <c>INTEGER</c>、<c>TEXT</c>）；可能为空。</summary>
        public string DeclaredType { get; }

        /// <summary>获取该列是否声明了 <c>NOT NULL</c>。</summary>
        public bool IsNotNull { get; }

        /// <summary>获取默认值 SQL 文本；没有默认值时为 null。</summary>
        public string? DefaultValue { get; }

        /// <summary>获取主键序号；0 表示不是主键。</summary>
        public int PrimaryKeyOrder { get; }
    }
}
