using System.Reflection;
using System.Runtime.CompilerServices;
using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>扫描结果：一个带 <see cref="EventHandlerAttribute"/> 的合法方法。</summary>
    internal sealed class EventHandlerDescriptor
    {
        public EventHandlerDescriptor(MethodInfo method, Type eventType, int order, string name)
        {
            Method = method;
            EventType = eventType;
            Order = order;
            Name = name;
        }

        public MethodInfo Method { get; }
        public Type EventType { get; }
        public int Order { get; }
        public string Name { get; }
    }

    /// <summary>两阶段扫描：先收集并校验全部方法，全部通过后才交给调用方注册。</summary>
    internal static class EventHandlerScanner
    {
        public static IReadOnlyList<EventHandlerDescriptor> Scan(object target, EventBusOptions options, int fallbackOrder)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(options);

            Type targetType = target.GetType();
            var methods = new List<MethodInfo>();

            foreach (MethodInfo method in targetType.GetMethods(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<EventHandlerAttribute>(inherit: true) is not null)
                    methods.Add(method);
            }

            // 反射返回顺序未定义；按元数据令牌固定注册顺序。
            methods.Sort((left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

            var descriptors = new List<EventHandlerDescriptor>(methods.Count);
            foreach (MethodInfo method in methods)
                descriptors.Add(Validate(method, options, fallbackOrder));

            return descriptors;
        }

        private static EventHandlerDescriptor Validate(MethodInfo method, EventBusOptions options, int fallbackOrder)
        {
            Type targetType = method.DeclaringType ?? typeof(object);
            string displayName = $"{targetType.Name}.{method.Name}";

            if (method.IsStatic)
                throw new InvalidOperationException($"[EventHandler] 不支持静态方法：{displayName}。");

            if (method.IsGenericMethodDefinition)
                throw new InvalidOperationException($"[EventHandler] 不支持泛型方法：{displayName}。");

            Type returnType = method.ReturnType;
            if (returnType != typeof(void) && returnType != typeof(Task) && returnType != typeof(ValueTask))
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 的返回类型必须是 void、Task 或 ValueTask，当前为 {returnType.Name}。");

            if (returnType == typeof(void) && method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
                throw new InvalidOperationException($"[EventHandler] 不支持 async void 方法：{displayName}。请改为返回 Task。");

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length is < 1 or > 2)
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 必须有 1 个事件参数，或 1 个事件参数 + CancellationToken。");

            ParameterInfo eventParameter = parameters[0];
            if (eventParameter.ParameterType.IsByRef ||
                eventParameter.IsOut ||
                eventParameter.GetCustomAttribute<ParamArrayAttribute>() is not null)
            {
                throw new InvalidOperationException($"[EventHandler] 方法 {displayName} 的事件参数不能是 ref/out/in/params。");
            }

            Type eventType = eventParameter.ParameterType;
            if (!typeof(IEvent).IsAssignableFrom(eventType))
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 的第一个参数必须实现 {nameof(IEvent)}，当前为 {eventType.Name}。");

            if (eventType == typeof(IEvent))
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 不能直接订阅 IEvent；请使用具体事件类型或基类。");

            if (eventType.IsInterface && !options.IncludeInterfaces)
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 订阅了事件接口 {eventType.Name}，需要 EventBusOptions.IncludeInterfaces = true。");

            if (eventType.IsAbstract && !eventType.IsInterface && !options.IncludeBaseTypes)
                throw new InvalidOperationException(
                    $"[EventHandler] 方法 {displayName} 订阅了抽象事件 {eventType.Name}，需要 EventBusOptions.IncludeBaseTypes = true。");

            if (parameters.Length == 2)
            {
                ParameterInfo tokenParameter = parameters[1];
                if (tokenParameter.ParameterType.IsByRef ||
                    tokenParameter.IsOut ||
                    tokenParameter.GetCustomAttribute<ParamArrayAttribute>() is not null ||
                    tokenParameter.ParameterType != typeof(CancellationToken))
                {
                    throw new InvalidOperationException(
                        $"[EventHandler] 方法 {displayName} 的第二个参数必须是 CancellationToken。");
                }
            }

            EventHandlerAttribute attribute = method.GetCustomAttribute<EventHandlerAttribute>(inherit: true)!;
            int order = attribute.Order == int.MinValue ? fallbackOrder : attribute.Order;
            return new EventHandlerDescriptor(method, eventType, order, displayName);
        }
    }
}
