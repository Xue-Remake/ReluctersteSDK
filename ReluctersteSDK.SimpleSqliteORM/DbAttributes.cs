namespace ReluctersteSDK.SimpleSqliteORM
{
    /// <summary>
    /// 标记实体类对应的数据库表名。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DbTableAttribute : Attribute
    {
        /// <summary>表名。</summary>
        public string? Name { get; }
        /// <summary>使用类名作为表名。</summary>
        public DbTableAttribute()
        {
        }
        /// <summary>指定表名。</summary>
        /// <param name="name">表名。</param>
        public DbTableAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性为数据库主键（或联合主键的一部分）。
    /// </summary>
    /// <remarks>带此特性的属性必定会映射到数据库列，且默认不可为空。</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DbKeyAttribute : Attribute
    {
        /// <summary>列名（若未指定则使用属性名）。</summary>
        public string? Name { get; }
        /// <summary>使用属性名作为列名。</summary>
        public DbKeyAttribute()
        {
        }
        /// <summary>指定列名。</summary>
        /// <param name="name">列名。</param>
        public DbKeyAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性映射到数据库列。
    /// </summary>
    /// <remarks>若属性同时具有 <see cref="DbKeyAttribute"/>，则以键列处理。</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DbColumnAttribute : Attribute
    {
        /// <summary>列名（若未指定则使用属性名）。</summary>
        public string? Name { get; }
        /// <summary>使用属性名作为列名。</summary>
        public DbColumnAttribute()
        {
        }
        /// <summary>指定列名。</summary>
        /// <param name="name">列名。</param>
        public DbColumnAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性，使其不参与数据库映射（忽略该属性）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DbIgnoreAttribute : Attribute
    {
    }
}
