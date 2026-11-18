using System.Collections.Concurrent;
using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>按运行时事件类型计算分发键：精确类型、基类链与可选接口。</summary>
    internal sealed class EventTypeResolver
    {
        private readonly EventBusOptions _options;
        private readonly ConcurrentDictionary<Type, Type[]> _cache = new();

        public EventTypeResolver(EventBusOptions options)
        {
            _options = options;
        }

        public Type[] Resolve(Type runtimeType) => _cache.GetOrAdd(runtimeType, Build);

        private Type[] Build(Type runtimeType)
        {
            var types = new List<Type> { runtimeType };

            if (_options.IncludeBaseTypes)
            {
                for (Type? baseType = runtimeType.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    if (baseType == typeof(object) || baseType == typeof(ValueType))
                        break;

                    if (typeof(IEvent).IsAssignableFrom(baseType))
                        types.Add(baseType);
                }
            }

            if (_options.IncludeInterfaces)
            {
                foreach (Type interfaceType in runtimeType.GetInterfaces())
                {
                    if (interfaceType != typeof(IEvent) && typeof(IEvent).IsAssignableFrom(interfaceType))
                        types.Add(interfaceType);
                }
            }

            return types.ToArray();
        }
    }
}
