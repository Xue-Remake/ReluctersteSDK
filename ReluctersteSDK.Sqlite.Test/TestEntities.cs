namespace ReluctersteSDK.Sqlite.Test
{
    /// <summary>
    /// 用于验证迁移建表与特性映射 CRUD 的实体。
    /// </summary>
    [DbTable("items")]
    public sealed class ItemEntity
    {
        [DbKey("id")]
        public long Id { get; set; }

        [DbColumn("name")]
        public string Name { get; set; } = string.Empty;

        [DbColumn("score")]
        public int Score { get; set; }
    }
}
