using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Paths;

namespace ReluctersteSDK.Config.Test
{
    /// <summary>
    /// 内存与 JSON 文件存储的读写、查询与错误语义测试。
    /// </summary>
    public class ConfigStoreTests
    {
        public sealed class Setting : IConfig
        {
            public string Id { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        private static string CreateTempFilePath() => System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "relucterste-config-test",
            Guid.NewGuid().ToString("N"),
            "settings.json");

        [Fact]
        public async Task 内存存储_保存后可按主键读取()
        {
            var store = new InMemoryConfigStore<Setting>(s => s.Id);

            ConfigSaveResult result = await store.SaveAsync(new Setting { Id = "a", Value = 1 });

            Assert.True(result.Success);
            Assert.Equal(1, result.Written);

            Setting? loaded = await store.GetAsync("a");
            Assert.NotNull(loaded);
            Assert.Equal(1, loaded!.Value);
        }

        [Fact]
        public async Task 内存存储_Insert模式遇到重复键整体失败()
        {
            var store = new InMemoryConfigStore<Setting>(s => s.Id);
            await store.SaveAsync(new Setting { Id = "a", Value = 1 });

            ConfigSaveResult result = await store.SaveAsync(
                new[] { new Setting { Id = "b", Value = 2 }, new Setting { Id = "a", Value = 3 } },
                SaveMode.Insert);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.Equal(0, result.Written);

            // 失败不应留下部分写入
            Assert.Null(await store.GetAsync("b"));
        }

        [Fact]
        public async Task 内存存储_查询支持谓词与条数限制()
        {
            var store = new InMemoryConfigStore<Setting>(s => s.Id);
            await store.SaveAsync(new[]
            {
                new Setting { Id = "a", Value = 1 },
                new Setting { Id = "b", Value = 2 },
                new Setting { Id = "c", Value = 3 },
            });

            IReadOnlyList<Setting> result =
                await store.QueryAsync(new ConfigQuery<Setting> { Predicate = s => s.Value > 1, Limit = 1 });

            Assert.Single(result);
            Assert.Equal(2, result[0].Value);
        }

        [Fact]
        public async Task JSON存储_写盘后新实例可读回()
        {
            string file = CreateTempFilePath();
            var first = new JsonFileConfigStore<Setting>(new AbsolutePath(file), s => s.Id);
            await first.SaveAsync(new Setting { Id = "a", Value = 7 });

            var second = new JsonFileConfigStore<Setting>(new AbsolutePath(file), s => s.Id);
            Setting? loaded = await second.GetAsync("a");

            Assert.NotNull(loaded);
            Assert.Equal(7, loaded!.Value);
            Assert.True(File.Exists(file));
            Assert.False(File.Exists(file + ".tmp"));
        }

        [Fact]
        public async Task JSON存储_查询与覆盖()
        {
            string file = CreateTempFilePath();
            var store = new JsonFileConfigStore<Setting>(new AbsolutePath(file), s => s.Id);

            await store.SaveAsync(new[] { new Setting { Id = "a", Value = 1 }, new Setting { Id = "b", Value = 2 } });
            await store.SaveAsync(new Setting { Id = "a", Value = 9 }, SaveMode.Upsert);

            Assert.Equal(9, (await store.GetAsync("a"))!.Value);
            Assert.Single(await store.QueryAsync(new ConfigQuery<Setting> { Predicate = s => s.Value > 5 }));
        }

        [Fact]
        public async Task 取消令牌_已取消时抛出()
        {
            var store = new InMemoryConfigStore<Setting>(s => s.Id);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync("a", cts.Token));
        }
    }
}
