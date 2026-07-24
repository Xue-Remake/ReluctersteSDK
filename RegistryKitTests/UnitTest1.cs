using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace RegistryKit.Tests
{
    #region 测试用辅助类与接口

    public interface ITestService
    {
        string GetMessage();
    }

    public class TestServiceA : ITestService
    {
        public string GetMessage() => "ServiceA";
    }

    public class TestServiceB : ITestService
    {
        public string GetMessage() => "ServiceB";
    }

    public class ThrowingService : ITestService
    {
        public ThrowingService()
        {
            throw new InvalidOperationException("Constructor failed");
        }

        public string GetMessage() => "Throwing";
    }

    #endregion

    #region 1. ValueRegistry Tests

    public class ValueRegistryTests
    {
        [Fact]
        public void Register_And_TryGetValue_ShouldWorkCorrectly()
        {
            var registry = new ValueRegistry<string, int>();

            registry.Register("key1", 100);

            Assert.True(registry.TryGetValue("key1", out var value));
            Assert.Equal(100, value);
        }

        [Fact]
        public void Register_ExistingKey_ShouldUpdateValueAndKeepEnabledState()
        {
            var registry = new ValueRegistry<string, string>();
            registry.Register("k1", "v1");
            registry.Disable("k1");

            // 覆盖已禁用的 key，应当保持禁用状态
            registry.Register("k1", "v2");

            Assert.False(registry.IsEnabled("k1"));
            Assert.False(registry.TryGetValue("k1", out _));

            // 重新启用后，值应当更新为 v2
            registry.Enable("k1");
            Assert.True(registry.TryGetValue("k1", out var val));
            Assert.Equal("v2", val);
        }

        [Fact]
        public void Enable_And_Disable_ShouldChangeStateCorrectly()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("k1", 10);

            Assert.True(registry.IsEnabled("k1"));

            Assert.True(registry.Disable("k1"));
            Assert.False(registry.IsEnabled("k1"));
            Assert.False(registry.TryGetValue("k1", out _));

            Assert.True(registry.Enable("k1"));
            Assert.True(registry.IsEnabled("k1"));
            Assert.True(registry.TryGetValue("k1", out var val));
            Assert.Equal(10, val);
        }

        [Fact]
        public void Enable_Disable_NonExistentKey_ShouldReturnFalse()
        {
            var registry = new ValueRegistry<string, int>();

            Assert.False(registry.Enable("not_exists"));
            Assert.False(registry.Disable("not_exists"));
        }

        [Fact]
        public void TryGetValue_WithPredicate_ShouldReturnMatchingEnabledValuesOnly()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 10);
            registry.Register("b", 20);
            registry.Register("c", 30);
            registry.Disable("c"); // 禁用 30

            // 筛选 > 15 的数值
            bool hasMatches = registry.TryGetValue(val => val > 15, out var values);

            Assert.True(hasMatches);
            Assert.Single(values);
            Assert.Contains(20, values);
            Assert.DoesNotContain(30, values);
        }

        [Fact]
        public void TryGetValue_WithPredicate_NoMatches_ShouldReturnFalseAndEmptyList()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("a", 10);

            bool hasMatches = registry.TryGetValue(val => val > 100, out var values);

            Assert.False(hasMatches);
            Assert.NotNull(values);
            Assert.Empty(values);
        }

        [Fact]
        public void Keys_ShouldReturnOnlyEnabledKeys()
        {
            var registry = new ValueRegistry<string, string>();
            registry.Register("k1", "v1");
            registry.Register("k2", "v2");
            registry.Disable("k2");

            var keys = registry.Keys();

            Assert.Single(keys);
            Assert.Contains("k1", keys);
        }

        [Fact]
        public void Remove_Clear_Exists_ShouldBehaveCorrectly()
        {
            var registry = new ValueRegistry<string, int>();
            registry.Register("k1", 1);
            registry.Register("k2", 2);

            Assert.True(registry.Exists("k1"));

            Assert.True(registry.Remove("k1"));
            Assert.False(registry.Exists("k1"));
            Assert.False(registry.Remove("k1"));

            registry.Clear();
            Assert.False(registry.Exists("k2"));
        }

        [Fact]
        public void NullArgument_ShouldThrowArgumentNullException()
        {
            var registry = new ValueRegistry<string, int>();

            Assert.Throws<ArgumentNullException>(() => registry.Register(null!, 1));
            Assert.Throws<ArgumentNullException>(() => registry.Enable(null!));
            Assert.Throws<ArgumentNullException>(() => registry.Disable(null!));
            Assert.Throws<ArgumentNullException>(() => registry.TryGetValue((string)null!, out _));
            Assert.Throws<ArgumentNullException>(() => registry.TryGetValue((Func<int, bool>)null!, out _));
            Assert.Throws<ArgumentNullException>(() => registry.Remove(null!));
            Assert.Throws<ArgumentNullException>(() => registry.Exists(null!));
            Assert.Throws<ArgumentNullException>(() => registry.IsEnabled(null!));
        }
    }

    #endregion

    #region 2. InstanceRegistry Tests

    public class InstanceRegistryTests
    {
        [Fact]
        public void RegisterInstance_And_Resolve_ShouldReturnSameInstance()
        {
            var registry = new InstanceRegistry();
            var service = new TestServiceA();

            registry.RegisterInstance<ITestService>(service);

            var resolved = registry.Resolve<ITestService>();
            Assert.Same(service, resolved);
        }

        [Fact]
        public void RegisterFactory_ShouldLazyCreateAndCacheSingletonInstance()
        {
            var registry = new InstanceRegistry();
            int factoryCallCount = 0;

            registry.RegisterFactory<ITestService>(() =>
            {
                factoryCallCount++;
                return new TestServiceA();
            });

            Assert.Equal(0, factoryCallCount);

            var inst1 = registry.Resolve<ITestService>();
            Assert.Equal(1, factoryCallCount);

            var inst2 = registry.Resolve<ITestService>();
            Assert.Equal(1, factoryCallCount); // 从缓存获取，不再增加调用次数
            Assert.Same(inst1, inst2);
        }

        [Fact]
        public void RegisterInstance_Overwrites_AndKeepsEnabled()
        {
            var registry = new InstanceRegistry();
            var service1 = new TestServiceA();
            var service2 = new TestServiceB();

            registry.RegisterInstance<ITestService>(service1);
            registry.Disable<ITestService>();

            // 覆盖已禁用的类型，保留启用状态（即依然为 false）
            registry.RegisterInstance<ITestService>(service2);
            Assert.False(registry.IsEnabled<ITestService>());

            registry.Enable<ITestService>();
            var resolved = registry.Resolve<ITestService>();
            Assert.Same(service2, resolved);
        }

        [Fact]
        public void Resolve_DisabledOrUnregisteredType_ShouldThrowInvalidOperationException()
        {
            var registry = new InstanceRegistry();
            registry.RegisterInstance<ITestService>(new TestServiceA());

            registry.Disable<ITestService>();

            Assert.Throws<InvalidOperationException>(() => registry.Resolve<ITestService>());
            Assert.Throws<InvalidOperationException>(() => registry.Resolve<string>());
        }

        [Fact]
        public void TryResolve_ShouldReturnFalse_WhenDisabledOrUnregistered()
        {
            var registry = new InstanceRegistry();
            registry.RegisterInstance<ITestService>(new TestServiceA());

            registry.Disable<ITestService>();

            Assert.False(registry.TryResolve<ITestService>(out var disabledVal));
            Assert.Null(disabledVal);

            Assert.False(registry.TryResolve<string>(out var unregVal));
            Assert.Null(unregVal);
        }

        [Fact]
        public void TryResolve_ShouldReturnTrue_WhenEnabled()
        {
            var registry = new InstanceRegistry();
            registry.RegisterInstance<ITestService>(new TestServiceA());

            Assert.True(registry.TryResolve<ITestService>(out var val));
            Assert.NotNull(val);
            Assert.Equal("ServiceA", val.GetMessage());
        }

        [Fact]
        public void EnabledTypes_Remove_Clear_Exists_IsEnabled()
        {
            var registry = new InstanceRegistry();
            registry.RegisterInstance<ITestService>(new TestServiceA());
            registry.RegisterInstance<string>("Hello");

            Assert.True(registry.Exists<ITestService>());
            Assert.True(registry.IsEnabled<ITestService>());

            registry.Disable<string>();
            var enabledTypes = registry.EnabledTypes();

            Assert.Single(enabledTypes);
            Assert.Contains(typeof(ITestService), enabledTypes);

            Assert.True(registry.Remove<ITestService>());
            Assert.False(registry.Exists<ITestService>());

            registry.Clear();
            Assert.False(registry.Exists<string>());
        }

        [Fact]
        public void ConcurrentResolve_WithFactory_ShouldOnlyCreateInstanceOnce()
        {
            var registry = new InstanceRegistry();
            int callCount = 0;

            registry.RegisterFactory<ITestService>(() =>
            {
                System.Threading.Thread.Sleep(20); // 模拟耗时创建
                System.Threading.Interlocked.Increment(ref callCount);
                return new TestServiceA();
            });

            var tasks = new Task<ITestService>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => registry.Resolve<ITestService>());
            }

            Task.WaitAll(tasks);

            Assert.Equal(1, callCount);
            for (int i = 1; i < tasks.Length; i++)
            {
                Assert.Same(tasks[0].Result, tasks[i].Result);
            }
        }

        [Fact]
        public void RegisterNull_ShouldThrowArgumentNullException()
        {
            var registry = new InstanceRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.RegisterInstance<ITestService>(null!));
            Assert.Throws<ArgumentNullException>(() => registry.RegisterFactory<ITestService>(null!));
        }
    }

    #endregion

    #region 3. TypeRegistry Tests

    public class TypeRegistryTests
    {
        [Fact]
        public void Register_ConcreteType_ShouldCreateNewInstancesEachTime()
        {
            var registry = new TypeRegistry();
            registry.Register<ITestService, TestServiceA>();

            var inst1 = registry.CreateInstance<ITestService>();
            var inst2 = registry.CreateInstance<ITestService>();

            Assert.NotNull(inst1);
            Assert.NotNull(inst2);
            Assert.NotSame(inst1, inst2); // 每次创建新实例
            Assert.Equal("ServiceA", inst1.GetMessage());
        }

        [Fact]
        public void Register_Factory_ShouldInvokeFactoryOnCreateInstance()
        {
            var registry = new TypeRegistry();
            int count = 0;
            registry.Register<ITestService>(() =>
            {
                count++;
                return new TestServiceB();
            });

            var inst1 = registry.CreateInstance<ITestService>();
            var inst2 = registry.CreateInstance<ITestService>();

            Assert.Equal(2, count);
            Assert.Equal("ServiceB", inst1.GetMessage());
        }

        [Fact]
        public void CreateInstance_DisabledOrUnregistered_ShouldThrowInvalidOperationException()
        {
            var registry = new TypeRegistry();
            registry.Register<ITestService, TestServiceA>();
            registry.Disable<ITestService>();

            Assert.Throws<InvalidOperationException>(() => registry.CreateInstance<ITestService>());
            Assert.Throws<InvalidOperationException>(() => registry.CreateInstance<string>());
        }

        [Fact]
        public void TryCreateInstance_WhenConstructionFails_ShouldReturnFalse()
        {
            var registry = new TypeRegistry();
            registry.Register<ITestService, ThrowingService>();

            bool success = registry.TryCreateInstance<ITestService>(out var instance);

            Assert.False(success);
            Assert.Null(instance);
        }

        [Fact]
        public void TryCreateInstance_Success_ShouldReturnTrueAndInstance()
        {
            var registry = new TypeRegistry();
            registry.Register<ITestService, TestServiceA>();

            bool success = registry.TryCreateInstance<ITestService>(out var instance);

            Assert.True(success);
            Assert.NotNull(instance);
        }

        [Fact]
        public void EnabledAbstractTypes_Remove_Clear_Exists_IsEnabled()
        {
            var registry = new TypeRegistry();
            registry.Register<ITestService, TestServiceA>();

            Assert.True(registry.Exists<ITestService>());
            Assert.True(registry.IsEnabled<ITestService>());

            var types = registry.EnabledAbstractTypes();
            Assert.Single(types);
            Assert.Contains(typeof(ITestService), types);

            Assert.True(registry.Disable<ITestService>());
            Assert.False(registry.IsEnabled<ITestService>());

            Assert.True(registry.Remove<ITestService>());
            Assert.False(registry.Exists<ITestService>());
        }

        [Fact]
        public void RegisterNullFactory_ShouldThrowArgumentNullException()
        {
            var registry = new TypeRegistry();
            Assert.Throws<ArgumentNullException>(() => registry.Register<ITestService>(null!));
        }
    }

    #endregion
}
