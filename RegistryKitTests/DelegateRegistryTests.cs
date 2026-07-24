using System;
using System.Collections.Generic;
using Xunit;

namespace RegistryKit.Tests
{
    // 定义自定义委托，用于测试 DelegateRegistry 对非 Action/Func 自定义委托的防御性 DynamicInvoke 支持
    public delegate void CustomVoidDelegate();
    public delegate string CustomStringDelegate();
    public delegate int CustomMathDelegate(int a, int b);

    public class DelegateRegistryTests
    {
        #region 1. 基础 Action / Func 注册与调用

        [Fact]
        public void Invoke_NoArgsAction_LambdaDirect_ShouldExecuteSuccessfully()
        {
            // 测试原本失败的场景：直接向 Register(name, del) 传递 Lambda 表达式
            var registry = new DelegateRegistry();
            bool executed = false;

            // 编译器可能将其推导为通用 Delegate 或 Action
            registry.Register("Action1", () => executed = true);
            registry.Invoke("Action1");

            Assert.True(executed);
        }

        [Fact]
        public void Invoke_NoArgsFunc_ShouldReturnCorrectValue()
        {
            var registry = new DelegateRegistry();

            registry.Register("Func1", () => "Hello World");
            string result = registry.Invoke<string>("Func1");

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void Invoke_OneArgAction_ShouldPassArgument()
        {
            var registry = new DelegateRegistry();
            int receivedVal = 0;

            registry.Register<int>("SetVal", x => receivedVal = x);
            registry.Invoke("SetVal", 42);

            Assert.Equal(42, receivedVal);
        }

        [Fact]
        public void Invoke_OneArgFunc_ShouldReturnCalculatedValue()
        {
            var registry = new DelegateRegistry();

            registry.Register<int, int>("Square", x => x * x);
            int result = registry.Invoke<int, int>("Square", 5);

            Assert.Equal(25, result);
        }

        #endregion

        #region 2. 防御性测试：自定义委托与类型转换降级 (DynamicInvoke Fallback)

        [Fact]
        public void Invoke_CustomVoidDelegate_ShouldFallbackToDynamicInvoke()
        {
            var registry = new DelegateRegistry();
            bool executed = false;

            // 注册自定义委托类型的实例（非 System.Action）
            CustomVoidDelegate customDel = new CustomVoidDelegate(() => executed = true);
            registry.Register("CustomVoid", customDel);

            // 触发 Invoke() 防御降级逻辑
            var exception = Record.Exception(() => registry.Invoke("CustomVoid"));

            Assert.Null(exception);
            Assert.True(executed);
        }

        [Fact]
        public void Invoke_CustomStringDelegate_ShouldFallbackToDynamicInvokeAndReturn()
        {
            var registry = new DelegateRegistry();

            CustomStringDelegate customDel = new CustomStringDelegate(() => "CustomResult");
            registry.Register("CustomString", customDel);

            // 触发 Invoke<TResult>() 防御降级逻辑
            string result = registry.Invoke<string>("CustomString");

            Assert.Equal("CustomResult", result);
        }

        [Fact]
        public void Invoke_TypeMismatchInDynamicInvoke_ShouldThrowInvalidOperationException()
        {
            var registry = new DelegateRegistry();

            // 注册接收 int 的委托，但后续尝试传入 string 类型的参数调用
            Func<int, int> func = x => x * 2;
            registry.Register("FuncInt", (Delegate)func);

            // 应该捕获 DynamicInvoke 抛出的异常并包装为 InvalidOperationException
            var ex = Assert.Throws<InvalidOperationException>(() => registry.Invoke<string, int>("FuncInt", "invalid_string_param"));

            Assert.Contains("调用委托 'FuncInt' 失败", ex.Message);
            Assert.NotNull(ex.InnerException); // 检查内部包含原生的 DynamicInvoke/TargetInvocationException
        }

        [Fact]
        public void Invoke_DelegateThrowsException_ShouldWrapInInvalidOperationException()
        {
            var registry = new DelegateRegistry();

            // 注册一个会抛出异常的自定义委托
            CustomVoidDelegate throwingDel = new CustomVoidDelegate(() => throw new FormatException("Invalid Format"));
            registry.Register("ThrowingDel", throwingDel);

            var ex = Assert.Throws<InvalidOperationException>(() => registry.Invoke("ThrowingDel"));

            Assert.Contains("调用委托 'ThrowingDel' 失败", ex.Message);
            Assert.IsType<FormatException>(ex.InnerException?.InnerException ?? ex.InnerException);
        }

        #endregion

        #region 3. 状态管理：Enable / Disable / Exists / IsEnabled

        [Fact]
        public void Invoke_DisabledDelegate_ShouldThrowInvalidOperationException()
        {
            var registry = new DelegateRegistry();
            registry.Register("Test", () => { });
            registry.Disable("Test");

            var ex = Assert.Throws<InvalidOperationException>(() => registry.Invoke("Test"));
            Assert.Contains("未启用", ex.Message);
        }

        [Fact]
        public void Invoke_UnregisteredDelegate_ShouldThrowInvalidOperationException()
        {
            var registry = new DelegateRegistry();

            var ex = Assert.Throws<InvalidOperationException>(() => registry.Invoke("NotExist"));
            Assert.Contains("未启用", ex.Message);
        }

        [Fact]
        public void Enable_Disable_ShouldToggleStatusCorrectly()
        {
            var registry = new DelegateRegistry();
            registry.Register("d1", () => { });

            Assert.True(registry.IsEnabled("d1"));

            Assert.True(registry.Disable("d1"));
            Assert.False(registry.IsEnabled("d1"));

            Assert.True(registry.Enable("d1"));
            Assert.True(registry.IsEnabled("d1"));
        }

        [Fact]
        public void Enable_Disable_NonExistentName_ShouldReturnFalse()
        {
            var registry = new DelegateRegistry();

            Assert.False(registry.Enable("unknown"));
            Assert.False(registry.Disable("unknown"));
        }

        [Fact]
        public void EnabledNames_ShouldReturnOnlyActiveDelegates()
        {
            var registry = new DelegateRegistry();
            registry.Register("d1", () => { });
            registry.Register("d2", () => { });
            registry.Register("d3", () => { });

            registry.Disable("d2");

            var activeNames = registry.EnabledNames();

            Assert.Equal(2, activeNames.Count);
            Assert.Contains("d1", activeNames);
            Assert.Contains("d3", activeNames);
            Assert.DoesNotContain("d2", activeNames);
        }

        #endregion

        #region 4. 集合管理：Remove / Clear / TryGetDelegate

        [Fact]
        public void TryGetDelegate_WhenEnabled_ShouldReturnTrueAndDelegate()
        {
            var registry = new DelegateRegistry();
            Action act = () => { };
            registry.Register("d1", act);

            Assert.True(registry.TryGetDelegate("d1", out var del));
            Assert.Same(act, del);
        }

        [Fact]
        public void TryGetDelegate_WhenDisabled_ShouldReturnFalseAndNull()
        {
            var registry = new DelegateRegistry();
            registry.Register("d1", () => { });
            registry.Disable("d1");

            Assert.False(registry.TryGetDelegate("d1", out var del));
            Assert.Null(del);
        }

        [Fact]
        public void Remove_Clear_ShouldRemoveEntriesCorrectly()
        {
            var registry = new DelegateRegistry();
            registry.Register("d1", () => { });
            registry.Register("d2", () => { });

            Assert.True(registry.Exists("d1"));

            Assert.True(registry.Remove("d1"));
            Assert.False(registry.Exists("d1"));
            Assert.False(registry.Remove("d1")); // 第二次移除返回 false

            registry.Clear();
            Assert.False(registry.Exists("d2"));
        }

        #endregion

        #region 5. 参数空值校验 (ArgumentNullException)

        [Fact]
        public void NullOrEmptyArgument_ShouldThrowArgumentNullException()
        {
            var registry = new DelegateRegistry();
            Action act = () => { };

            Assert.Throws<ArgumentNullException>(() => registry.Register(null!, act));
            Assert.Throws<ArgumentNullException>(() => registry.Register("", act));
            Assert.Throws<ArgumentNullException>(() => registry.Register("name", (Delegate)null!));
            Assert.Throws<ArgumentNullException>(() => registry.Enable(null!));
            Assert.Throws<ArgumentNullException>(() => registry.Disable(null!));
            Assert.Throws<ArgumentNullException>(() => registry.TryGetDelegate(null!, out _));
            Assert.Throws<ArgumentNullException>(() => registry.Remove(null!));
            Assert.Throws<ArgumentNullException>(() => registry.Exists(null!));
            Assert.Throws<ArgumentNullException>(() => registry.IsEnabled(null!));
        }

        #endregion
    }
}
