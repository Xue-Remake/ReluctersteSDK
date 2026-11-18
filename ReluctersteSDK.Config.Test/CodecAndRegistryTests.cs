using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.Config.Codecs;
using ReluctersteSDK.Config.Hosting;
using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;
using ReluctersteSDK.Paths;

namespace ReluctersteSDK.Config.Test
{
    /// <summary>
    /// 显式编解码器（源生成）与配置节注册表的测试。
    /// </summary>
    public partial class CodecAndRegistryTests
    {
        public sealed class CodecSetting : IConfig
        {
            public string Id { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        public sealed class NotDeclaredSetting : IConfig
        {
            public string Id { get; set; } = string.Empty;
        }

        /// <summary>
        /// 源生成上下文：只声明 <see cref="CodecSetting"/>，不声明 <see cref="NotDeclaredSetting"/>。
        /// </summary>
        [JsonSourceGenerationOptions(WriteIndented = true)]
        [JsonSerializable(typeof(CodecSetting))]
        internal partial class TestJsonContext : JsonSerializerContext
        {
        }

        private static string CreateTempFilePath() => System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "relucterste-config-codec-test",
            Guid.NewGuid().ToString("N"),
            "settings.json");

        [Fact]
        public async Task 编解码器_源生成往返()
        {
            string file = CreateTempFilePath();
            var codec = JsonConfigCodec<CodecSetting>.FromContext(TestJsonContext.Default, s => s.Id);

            var writer = new JsonFileConfigStore<CodecSetting>(new AbsolutePath(file), codec);
            ConfigSaveResult saved = await writer.SaveAsync(new CodecSetting { Id = "a", Value = 4 });
            Assert.True(saved.Success, saved.Error?.Message);

            var reader = new JsonFileConfigStore<CodecSetting>(new AbsolutePath(file), codec);
            CodecSetting? loaded = await reader.GetAsync("a");

            Assert.NotNull(loaded);
            Assert.Equal(4, loaded!.Value);
            Assert.False(File.Exists(file + ".tmp"));
        }

        [Fact]
        public void 编解码器_未声明的类型拒绝创建()
        {
            Assert.ThrowsAny<Exception>(() =>
                JsonConfigCodec<NotDeclaredSetting>.FromContext(TestJsonContext.Default, s => s.Id));
        }

        [Fact]
        public async Task 配置节注册表_注册与读取()
        {
            var registry = new ConfigRegistry();
            var store = new InMemoryConfigStore<CodecSetting>(s => s.Id);
            registry.Register(store, "game");

            Assert.True(registry.TryGetStore<CodecSetting>(out IConfigStore<CodecSetting> resolved));
            Assert.NotNull(resolved);
            Assert.Contains("game", registry.SectionNames);
            Assert.True(registry.TryGetConfigType("game", out Type type));
            Assert.Equal(typeof(CodecSetting), type);
        }

        [Fact]
        public void 配置节注册表_重复注册抛异常()
        {
            var registry = new ConfigRegistry();
            registry.Register(new InMemoryConfigStore<CodecSetting>(s => s.Id), "game");

            Assert.Throws<InvalidOperationException>(
                () => registry.Register(new InMemoryConfigStore<CodecSetting>(s => s.Id)));
        }

        [Fact]
        public void 配置节注册表_未注册取用抛异常()
        {
            var registry = new ConfigRegistry();

            Assert.False(registry.TryGetStore<CodecSetting>(out _));
            Assert.Throws<InvalidOperationException>(() => registry.GetStore<CodecSetting>());
        }

        [Fact]
        public async Task 配置节注册表_保存成功触发变更事件()
        {
            var registry = new ConfigRegistry();
            registry.Register(new InMemoryConfigStore<CodecSetting>(s => s.Id), "game");

            ConfigChangedEventArgs? captured = null;
            registry.Changed += (_, e) => captured = e;

            await registry.GetStore<CodecSetting>().SaveAsync(new CodecSetting { Id = "a", Value = 1 });

            Assert.NotNull(captured);
            Assert.Equal("game", captured!.Section);
            Assert.Equal(typeof(CodecSetting), captured.ConfigType);
            Assert.Equal(1, captured.Written);
            Assert.Equal(SaveMode.Upsert, captured.Mode);
        }

        [Fact]
        public async Task 配置节注册表_保存失败不触发事件()
        {
            var registry = new ConfigRegistry();
            registry.Register(new InMemoryConfigStore<CodecSetting>(s => s.Id), "game");
            await registry.GetStore<CodecSetting>().SaveAsync(new CodecSetting { Id = "a", Value = 1 });

            bool raised = false;
            registry.Changed += (_, _) => raised = true;

            ConfigSaveResult result = await registry
                .GetStore<CodecSetting>()
                .SaveAsync(new CodecSetting { Id = "a", Value = 2 }, SaveMode.Insert);

            Assert.False(result.Success);
            Assert.False(raised);
        }

        [Fact]
        public void DI扩展_注册单例注册表()
        {
            var services = new ServiceCollection();
            services.AddReluctersteSDKConfig(
                registry => registry.Register(new InMemoryConfigStore<CodecSetting>(s => s.Id), "game"));

            using ServiceProvider provider = services.BuildServiceProvider();

            ConfigRegistry registry = provider.GetRequiredService<ConfigRegistry>();
            Assert.True(registry.TryGetStore<CodecSetting>(out _));
            Assert.Same(registry, provider.GetRequiredService<ConfigRegistry>());
        }
    }
}
