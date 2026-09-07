using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 事件总线的基础实现，支持基于 DI 容器的类型分发及委托即时分发
    /// </summary>
    public class EventBusBase : IEventBus
    {
        /// <summary>
        /// 维护事件类型到处理器类型集合的映射映射字典（基于类型的订阅）
        /// </summary>
        protected readonly ConcurrentDictionary<Type, HashSet<Type>> TypeHandlers = new();
        /// <summary>
        /// 维护事件类型到委托处理器列表的映射字典（基于委托的订阅）
        /// </summary>
        protected readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> DynamicHandlers = new();
        /// <summary>
        /// 依赖注入服务提供程序，用于解析 Scoped 或 Transient 生命周期的处理器实例
        /// </summary>
        protected readonly IServiceProvider? ServiceProvider;

        /// <summary>
        /// 初始化 <see cref="EventBusBase"/> 类的新实例
        /// </summary>
        /// <param name="serviceProvider">可选的依赖注入服务提供者</param>
        public EventBusBase(IServiceProvider? serviceProvider = null)
        {
            ServiceProvider = serviceProvider;
        }

        #region 订阅管理
        /// <summary>
        /// 注册基于类型的事件处理器
        /// </summary>
        /// <typeparam name="TEvent">目标事件类型</typeparam>
        /// <typeparam name="THandler">处理事件的实现类类型</typeparam>
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
        /// <summary>
        /// 注销已注册的特定事件处理器类型
        /// </summary>
        /// <typeparam name="TEvent">目标事件类型</typeparam>
        /// <typeparam name="THandler">待移除的处理器类型</typeparam>
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
        /// <summary>
        /// 注册基于委托回调的事件处理器
        /// </summary>
        /// <typeparam name="TEvent">目标事件类型</typeparam>
        /// <param name="handler">事件触发时执行的异步委托</param>
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
        /// <summary>
        /// 异步发布事件，并发广播通知所有匹配的订阅者（包括动态委托与类型处理器）
        /// </summary>
        /// <typeparam name="TEvent">发布的事件类型</typeparam>
        /// <param name="event">待发布的事件实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示所有处理器执行完毕的 Task</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="event"/> 为 null 时抛出</exception>
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
        /// <summary>
        /// 安全执行单个处理器，捕获并转交异常，防止单个订阅者崩溃影响其他订阅者的分发
        /// </summary>
        /// <param name="action">待执行的处理器调用操作</param>
        /// <param name="eventType">当前正在分发的事件类型</param>
        /// <returns>异步执行的 Task</returns>
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
        /// <summary>
        /// 当某个事件处理器在执行过程中抛出异常时的回调处理方法
        /// </summary>
        /// <param name="eventType">抛出异常的目标事件类型</param>
        /// <param name="ex">捕获到的异常实例</param>
        /// <returns>异步处理任务</returns>
        /// <remarks>
        /// 默认实现仅向控制台输出错误，派生类可重写此方法接入更完善的日志记录组件（如 <c>ILogger</c>）或告警系统。
        /// </remarks>
        protected virtual Task OnHandlerExceptionAsync(Type eventType, Exception ex)
        {
            // 默认控制台打印，子类可集成 ILogger
            Console.WriteLine($"[EventBus Error] Exception handling event {eventType.Name}: {ex.Message}");
            return Task.CompletedTask;
        }
        #endregion
    }
}
