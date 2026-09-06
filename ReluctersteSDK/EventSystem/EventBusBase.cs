using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.EventSystem
{
    public class EventBusBase : IEventBus
    {
        protected readonly ConcurrentDictionary<Type, HashSet<Type>> TypeHandlers = new();
        protected readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> DynamicHandlers = new();
        protected readonly IServiceProvider? ServiceProvider;

        public EventBusBase(IServiceProvider? serviceProvider = null)
        {
            ServiceProvider = serviceProvider;
        }

        #region 订阅管理
        public virtual void Subscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);
            TypeHandlers.AddOrUpdate(eventType,
                _ => new HashSet<Type> { handlerType },
                (_, handlers) =>
                {
                    lock (handlers)
                        handlers.Add(handlerType);
                    return handlers;
                });
        }
        public virtual void Unsubscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);
            if (TypeHandlers.TryGetValue(eventType, out var handlers))
            {
                lock (handlers)
                    handlers.Remove(handlerType);
            }
        }
        public virtual void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            Task Wrapper(object @event, CancellationToken ct) => handler((TEvent)@event, ct);
            DynamicHandlers.AddOrUpdate(eventType,
                _ => new List<Func<object, CancellationToken, Task>> { Wrapper },
                (_, list) =>
                {
                    lock (list)
                        list.Add(Wrapper);
                    return list;
                });
        }
        #endregion

        #region 事件发布与分发
        public virtual async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            var eventType = @event.GetType();
            var tasks = new List<Task>();
            // 1. 触发基于委托的处理器
            if (DynamicHandlers.TryGetValue(eventType, out var dynamicList))
            {
                List<Func<object, CancellationToken, Task>> copy;
                lock (dynamicList)
                {
                    copy = dynamicList.ToList();
                }
                foreach (var handler in copy)
                {
                    tasks.Add(ExecuteHandlerSafelyAsync(() => handler(@event, cancellationToken), eventType));
                }
            }
            // 2. 触发基于类型的处理器（通过 DI 容器解析）
            if (TypeHandlers.TryGetValue(eventType, out var handlerTypes))
            {
                List<Type> copy;
                lock (handlerTypes)
                    copy = handlerTypes.ToList();
                // 创建 Scope 处理 Scoped 依赖
                using var scope = ServiceProvider?.CreateScope();
                foreach (var handlerType in copy)
                {
                    tasks.Add(ExecuteHandlerSafelyAsync(async () =>
                    {
                        object? handlerInstance = null;
                        if (scope != null)
                        {
                            handlerInstance = scope.ServiceProvider.GetService(handlerType);
                        }
                        // 如果容器里没注册，尝试直接反射创建（要求有无参构造）
                        handlerInstance ??= Activator.CreateInstance(handlerType);
                        if (handlerInstance is IEventHandler<TEvent> typedHandler)
                        {
                            await typedHandler.HandleAsync(@event, cancellationToken);
                        }
                    }, eventType));
                }
            }
            await Task.WhenAll(tasks);
        }

        protected virtual async Task ExecuteHandlerSafelyAsync(Func<Task> action, Type eventType)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await OnHandlerExceptionAsync(eventType, ex);
            }
        }
        protected virtual Task OnHandlerExceptionAsync(Type eventType, Exception ex)
        {
            // 默认控制台打印，子类可集成 ILogger
            Console.WriteLine($"[EventBus Error] Exception handling event {eventType.Name}: {ex.Message}");
            return Task.CompletedTask;
        }
        #endregion
    }
}
