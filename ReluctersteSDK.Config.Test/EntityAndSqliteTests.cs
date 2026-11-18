using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Sqlite;

namespace ReluctersteSDK.Config.Test
{
    /// <summary>
    /// 实体适配器与 SQLite 存储测试。
    /// </summary>
    public class EntityAndSqliteTests
    {
        public sealed class ItemEntity : IItemEntity<ItemEntity, ItemData>
        {
            public int ItemId { get; set; }
            public string Name { get; set; } = string.Empty;

            public ItemData ToData() => new() { ItemId = ItemId, Name = Name };
        }

        public sealed class ItemData : IItemData<ItemEntity>
        {
            public int ItemId { get; set; }
            public string Name { get; set; } = string.Empty;

            public ItemEntity ToEntity() => new() { ItemId = ItemId, Name = Name };
        }

        [DbTable("config_setting")]
        public sealed class DbSetting : IConfig
        {
            [DbKey]
            public string Id { get; set; } = string.Empty;

            [DbColumn]
            public int Value { get; set; }
        }

        [Fact]
        public async Task 实体适配器_往返转换()
        {
            var store = new InMemoryConfigStore<ItemData>(d => d.ItemId);
            var entities = new EntityConfigStore<ItemEntity, ItemData>(store);

            ConfigSaveResult saved = await entities.SaveAsync(new ItemEntity { ItemId = 1, Name = "Sword" });
            Assert.True(saved.Success);

            ItemEntity? loaded = await entities.GetAsync(new object?[] { 1 });

            Assert.NotNull(loaded);
            Assert.Equal("Sword", loaded!.Name);
        }

        [Fact]
        public async Task SQLite存储_自动建表并读写()
        {
            string folder = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "relucterste-sqlite-test",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);

            await using var database = new SqliteDataBase(folder, "config.db");
            var store = new SqliteConfigStore<DbSetting>(database);

            ConfigSaveResult saved = await store.SaveAsync(new DbSetting { Id = "a", Value = 3 });
            Assert.True(saved.Success, saved.Error?.Message);

            DbSetting? loaded = await store.GetAsync("a");
            Assert.NotNull(loaded);
            Assert.Equal(3, loaded!.Value);

            IReadOnlyList<DbSetting> all = await store.QueryAsync();
            Assert.Single(all);
        }

        [Fact]
        public async Task SQLite存储_Insert重复键返回失败而不是抛异常()
        {
            string folder = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "relucterste-sqlite-test",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);

            await using var database = new SqliteDataBase(folder, "config.db");
            var store = new SqliteConfigStore<DbSetting>(database);

            await store.SaveAsync(new DbSetting { Id = "a", Value = 1 }, SaveMode.Insert);
            ConfigSaveResult second = await store.SaveAsync(new DbSetting { Id = "a", Value = 2 }, SaveMode.Insert);

            Assert.False(second.Success);
            Assert.NotNull(second.Error);
            Assert.Equal(0, second.Written);
        }
    }
}
