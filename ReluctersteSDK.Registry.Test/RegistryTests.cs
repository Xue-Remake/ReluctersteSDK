using System.Text.Json;
using ReluctersteSDK.Registry;
using ReluctersteSDK.Registry.Json;

namespace ReluctersteSDK.Registry.Test
{
    /// <summary>
    /// 五个注册表的注册、启停与 JSON 往返测试。
    /// </summary>
    public class RegistryTests
    {
        private interface IShape
        {
        }

        private sealed class Square : IShape
        {
        }

        [Fact]
        public void 值注册表_注册与启停()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 1);

            Assert.True(registry.TryGetValue("a", out int value));
            Assert.Equal(1, value);

            Assert.True(registry.Disable("a"));
            Assert.False(registry.TryGetValue("a", out _));

            Assert.True(registry.Enable("a"));
            Assert.True(registry.TryGetValue("a", out value));
            Assert.Equal(1, value);
        }

        [Fact]
        public void 值注册表_覆盖时保留禁用状态()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 1);
            registry.Disable("a");

            registry.Register("a", 5);

            Assert.False(registry.IsEnabled("a"));
            Assert.False(registry.TryGetValue("a", out _));
        }

        [Fact]
        public void 值注册表_查询只返回启用项()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 1);
            registry.Register("b", 2);
            registry.Disable("b");

            IList<int> result = registry.Query(v => v > 0);

            Assert.Single(result);
            Assert.Equal(1, result[0]);
        }

        [Fact]
        public void 复合键注册表_按列注册与读取()
        {
            var registry = new CompositeValueRegistry<string>();
            registry.Register("k1", "k2", "v");

            Assert.Equal("v", registry.GetValue("k1", "k2"));
            Assert.True(registry.TryGetValue(new CompositeKey("k1", "k2"), out string? found));
            Assert.Equal("v", found);
        }

        [Fact]
        public void 实例注册表_工厂异常时TryResolve返回false()
        {
            var registry = new InstanceRegistry();
            registry.RegisterFactory<object>(() => throw new InvalidOperationException("boom"));

            Assert.False(registry.TryResolve<object>(out _));
            Assert.Throws<InvalidOperationException>(() => registry.Resolve<object>());
        }

        [Fact]
        public void 类型注册表_按抽象类型创建()
        {
            var registry = new TypeRegistry();
            registry.Register<IShape, Square>();

            IShape created = registry.CreateInstance<IShape>();

            Assert.IsType<Square>(created);
        }

        [Fact]
        public void 委托注册表_调用与禁用()
        {
            var registry = new DelegateRegistry();
            int counter = 0;
            registry.Register("inc", () => counter++);

            registry.Invoke("inc");
            Assert.Equal(1, counter);

            Assert.True(registry.Disable("inc"));
            Assert.Throws<InvalidOperationException>(() => registry.Invoke("inc"));
        }

        [Fact]
        public void JSON往返_保留值与启用状态()
        {
            var options = RegistryJson.CreateOptions();
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 1);
            registry.Register("b", 2);
            registry.Disable("b");

            string json = JsonSerializer.Serialize(registry, options);
            ValueRegistry<string, int>? restored =
                JsonSerializer.Deserialize<ValueRegistry<string, int>>(json, options);

            Assert.NotNull(restored);
            Assert.True(restored!.TryGetValue("a", out int value));
            Assert.Equal(1, value);
            Assert.False(restored.IsEnabled("b"));
        }
    }
}
