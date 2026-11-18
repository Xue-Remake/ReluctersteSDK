using System.Reflection;
using System.Runtime.CompilerServices;
using ReluctersteSDK.EventSystem.Interfaces;
using ReluctersteSDK.EventSystem.Internal;

namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 线程安全的异步事件总线。订阅表使用写时复制快照，发布路径无锁。
    /// </summary>
    /// <remarks>
    /// <para>默认分发范围是精确类型 + 基类；接口分发需要
    /// <see cref="EventBusOptions.IncludeInterfaces"/>。</para>
    /// <para>默认在全部处理器执行完后抛出 <see cref="EventDispatchException"/>；
    /// 只关心结果而不想处理异常时把 <see cref="EventBusOptions.ThrowOnError"/> 设为 <c>false</c>。</para>
    /// </remarks>
    public sealed class EventBus : IEventBus
    {
        private readonly EventBusOptions _options;
        private readonly HandlerInvokerFactory _invokerFactory;
        private readonly EventTypeResolver _typeResolver;
        private readonly object _writeLock = new();

        private Dictionary<Type, Subscription[]> _snapshot = new();
        private long _sequence;

        /// <summary>创建一个事件总线。</summary>
        /// <param name="serviceProvider">可选的依赖注入容器；用于创建类型处理器与 Scoped 依赖。</param>
        /// <param name="options">可选选项；为 <c>null</c> 时使用默认值。</param>
        /// <exception cref="ArgumentOutOfRangeException">选项中的超时时间非法。</exception>
        public EventBus(IServiceProvider? serviceProvider = null, EventBusOptions? options = null)
        {
            _options = options ?? new EventBusOptions();
            _options.Validate();
            _invokerFactory = new HandlerInvokerFactory(serviceProvider, _options);
            _typeResolver = new EventTypeResolver(_options);
        }

        /// <summary>使用指定选项创建一个不带依赖注入容器的事件总线。</summary>
        /// <param name="options">事件总线选项。</param>
        /// <exception cref="ArgumentOutOfRangeException">选项中的超时时间非法。</exception>
        public EventBus(EventBusOptions options)
            : this(null, options)
        {
        }

        #region 订阅

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent, THandler>(int order = 0)
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            EnsureDispatchable<TEvent>();

            Type handlerType = typeof(THandler);
            string handlerName = handlerType.FullName ?? handlerType.Name;
            var invoker = _invokerFactory.CreateTypeInvoker<TEvent, THandler>(handlerName);
            return AddCore(typeof(TEvent), invoker, SubscriptionKind.Type, handlerType, handlerName, null, null, order);
        }

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler, int order = 0)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureDispatchable<TEvent>();

            Type handlerType = handler.GetType();
            string handlerName = handlerType.FullName ?? handlerType.Name;
            var invoker = _invokerFactory.CreateInstanceInvoker(handler, handlerName);
            return AddCore(typeof(TEvent), invoker, SubscriptionKind.Instance, handlerType, handlerName, null, handler, order);
        }

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler, int order = 0)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureDispatchable<TEvent>();

            string handlerName = GetDelegateName(handler);
            var invoker = _invokerFactory.CreateDelegateInvoker(handler, handlerName);
            return AddCore(typeof(TEvent), invoker, SubscriptionKind.Delegate, null, handlerName, handler, null, order);
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler, int order = 0)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureDispatchable<TEvent>();
            EnsureNotAsyncVoid(handler);

            string handlerName = GetDelegateName(handler);
            var invoker = _invokerFactory.CreateDelegateInvoker<TEvent>(
                (eventObject, _) => handler(eventObject),
                handlerName);
            return AddCore(typeof(TEvent), invoker, SubscriptionKind.Delegate, null, handlerName, handler, null, order);
        }

        /// <inheritdoc />
        public IDisposable SubscribeSync<TEvent>(Action<TEvent> handler, int order = 0)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureDispatchable<TEvent>();
            EnsureNotAsyncVoid(handler);

            string handlerName = GetDelegateName(handler);
            var invoker = _invokerFactory.CreateDelegateInvoker<TEvent>(
                (eventObject, _) =>
                {
                    handler(eventObject);
                    return Task.CompletedTask;
                },
                handlerName);
            return AddCore(typeof(TEvent), invoker, SubscriptionKind.Delegate, null, handlerName, handler, null, order);
        }

        /// <inheritdoc />
        public IDisposable SubscribeFromTarget(object target, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (target.GetType().IsValueType)
                throw new ArgumentException("SubscribeFromTarget 不支持值类型目标；方法内的修改无法回写到原值。", nameof(target));

            IReadOnlyList<EventHandlerDescriptor> descriptors = EventHandlerScanner.Scan(target, _options, order);
            var tokens = new List<IDisposable>(descriptors.Count);

            try
            {
                foreach (EventHandlerDescriptor descriptor in descriptors)
                {
                    var invoker = _invokerFactory.CreateMethodInvoker(target, descriptor.Method, descriptor.Name);
                    tokens.Add(AddCore(
                        descriptor.EventType,
                        invoker,
                        SubscriptionKind.Attribute,
                        target.GetType(),
                        descriptor.Name,
                        null,
                        target,
                        descriptor.Order));
                }
            }
            catch
            {
                foreach (IDisposable token in tokens)
                    token.Dispose();
                throw;
            }

            return new CompositeDisposable(tokens);
        }

        #endregion

        #region 退订

        /// <inheritdoc />
        public int Unsubscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
            => RemoveWhere(typeof(TEvent), subscription =>
                subscription.Kind == SubscriptionKind.Type && subscription.HandlerType == typeof(THandler));

        /// <inheritdoc />
        public int Unsubscribe<TEvent>(IEventHandler<TEvent> handler)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            return RemoveWhere(typeof(TEvent), subscription =>
                subscription.Kind == SubscriptionKind.Instance && ReferenceEquals(subscription.HandlerInstance, handler));
        }

        /// <inheritdoc />
        public int Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            return RemoveWhere(typeof(TEvent), subscription =>
                subscription.Kind == SubscriptionKind.Delegate &&
                subscription.OriginalHandler is not null &&
                subscription.OriginalHandler.Equals(handler));
        }

        /// <inheritdoc />
        public int UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            return RemoveWhere(typeof(TEvent), subscription =>
                subscription.Kind == SubscriptionKind.Delegate &&
                subscription.OriginalHandler is not null &&
                subscription.OriginalHandler.Equals(handler));
        }

        /// <inheritdoc />
        public int UnsubscribeSync<TEvent>(Action<TEvent> handler)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(handler);
            return RemoveWhere(typeof(TEvent), subscription =>
                subscription.Kind == SubscriptionKind.Delegate &&
                subscription.OriginalHandler is not null &&
                subscription.OriginalHandler.Equals(handler));
        }

        /// <inheritdoc />
        public int Clear<TEvent>()
            where TEvent : IEvent
        {
            lock (_writeLock)
            {
                if (!_snapshot.TryGetValue(typeof(TEvent), out Subscription[]? existing))
                    return 0;

                var copy = new Dictionary<Type, Subscription[]>(_snapshot);
                copy.Remove(typeof(TEvent));
                Volatile.Write(ref _snapshot, copy);
                return existing.Length;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_writeLock)
                Volatile.Write(ref _snapshot, new Dictionary<Type, Subscription[]>());
        }

        #endregion

        #region 发布

        /// <inheritdoc />
        public async Task<EventDispatchResult> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent
        {
            ArgumentNullException.ThrowIfNull(@event);

            Type runtimeType = @event.GetType();
            Dictionary<Type, Subscription[]> snapshot = Volatile.Read(ref _snapshot);
            Type[] dispatchTypes = _typeResolver.Resolve(runtimeType);

            var subscriptions = new List<Subscription>();
            foreach (Type dispatchType in dispatchTypes)
            {
                if (snapshot.TryGetValue(dispatchType, out Subscription[]? items))
                    subscriptions.AddRange(items);
            }

            subscriptions.Sort(SubscriptionComparer.Instance);

            int matchedCount = subscriptions.Count;
            if (cancellationToken.IsCancellationRequested)
            {
                return new EventDispatchResult(
                    @event.EventId,
                    runtimeType,
                    matchedCount,
                    0,
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<EventHandlerError>(),
                    Array.Empty<Exception>(),
                    isCanceled: true,
                    isTimedOut: false);
            }

            var errors = new DispatchErrorCollector();
            int successCount = 0;
            int failedCount = 0;
            int canceledCount = 0;
            int timedOutCount = 0;
            int executedCount = 0;
            bool isCanceled = false;
            bool isTimedOut = false;

            if (_options.ExecutionMode == EventExecutionMode.Sequential)
            {
                foreach (Subscription subscription in subscriptions)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        isCanceled = true;
                        break;
                    }

                    HandlerOutcome outcome = await ExecuteSubscriptionAsync(subscription, @event, cancellationToken);
                    executedCount++;
                    RecordOutcome(
                        subscription,
                        outcome,
                        runtimeType,
                        errors,
                        ref successCount,
                        ref failedCount,
                        ref canceledCount,
                        ref timedOutCount,
                        ref isCanceled,
                        ref isTimedOut);

                    if (outcome.Kind == HandlerOutcomeKind.TimedOut)
                        break;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        isCanceled = true;
                        break;
                    }
                }
            }
            else
            {
                var tasks = new Task<HandlerOutcome>[subscriptions.Count];
                for (int i = 0; i < subscriptions.Count; i++)
                    tasks[i] = ExecuteSubscriptionAsync(subscriptions[i], @event, cancellationToken);

                HandlerOutcome[] outcomes = await Task.WhenAll(tasks);
                executedCount = outcomes.Length;
                for (int i = 0; i < outcomes.Length; i++)
                {
                    RecordOutcome(
                        subscriptions[i],
                        outcomes[i],
                        runtimeType,
                        errors,
                        ref successCount,
                        ref failedCount,
                        ref canceledCount,
                        ref timedOutCount,
                        ref isCanceled,
                        ref isTimedOut);
                }

                if (cancellationToken.IsCancellationRequested)
                    isCanceled = true;
            }

            IReadOnlyList<EventHandlerError> errorList = errors.ToOrderedList();
            var callbackErrors = new List<Exception>();
            if (_options.OnError is { } onError)
            {
                foreach (EventHandlerError error in errorList)
                {
                    try
                    {
                        onError(error);
                    }
                    catch (Exception ex)
                    {
                        callbackErrors.Add(ex);
                    }
                }
            }

            var result = new EventDispatchResult(
                @event.EventId,
                runtimeType,
                matchedCount,
                executedCount,
                successCount,
                failedCount,
                canceledCount,
                timedOutCount,
                errorList,
                callbackErrors,
                isCanceled,
                isTimedOut);

            if (_options.ThrowOnError && errorList.Count > 0)
                throw new EventDispatchException(result);

            return result;
        }

        #endregion

        #region 内部实现

        private static async Task<HandlerOutcome> ExecuteSubscriptionAsync(
            Subscription subscription,
            object eventObject,
            CancellationToken cancellationToken)
        {
            try
            {
                return await subscription.Invoke(eventObject, cancellationToken);
            }
            catch (Exception ex)
            {
                return HandlerOutcome.Error(ex);
            }
        }

        private static void RecordOutcome(
            Subscription subscription,
            HandlerOutcome outcome,
            Type runtimeType,
            DispatchErrorCollector errors,
            ref int successCount,
            ref int failedCount,
            ref int canceledCount,
            ref int timedOutCount,
            ref bool isCanceled,
            ref bool isTimedOut)
        {
            switch (outcome.Kind)
            {
                case HandlerOutcomeKind.Success:
                    successCount++;
                    break;

                case HandlerOutcomeKind.Canceled:
                    canceledCount++;
                    isCanceled = true;
                    break;

                case HandlerOutcomeKind.TimedOut:
                    timedOutCount++;
                    isTimedOut = true;
                    errors.Add(subscription, new EventHandlerError(
                        runtimeType,
                        subscription.EventType,
                        subscription.HandlerType,
                        subscription.HandlerName,
                        isTimeout: true,
                        outcome.Exception!));
                    break;

                case HandlerOutcomeKind.Error:
                    failedCount++;
                    errors.Add(subscription, new EventHandlerError(
                        runtimeType,
                        subscription.EventType,
                        subscription.HandlerType,
                        subscription.HandlerName,
                        isTimeout: false,
                        outcome.Exception!));
                    break;
            }
        }

        private IDisposable AddCore(
            Type eventType,
            Func<object, CancellationToken, Task<HandlerOutcome>> invoker,
            SubscriptionKind kind,
            Type? handlerType,
            string handlerName,
            Delegate? originalHandler,
            object? handlerInstance,
            int order)
        {
            long sequence = Interlocked.Increment(ref _sequence);
            var subscription = new Subscription(
                sequence,
                eventType,
                kind,
                handlerType,
                handlerName,
                originalHandler,
                handlerInstance,
                order,
                sequence,
                invoker);

            lock (_writeLock)
            {
                var copy = new Dictionary<Type, Subscription[]>(_snapshot);
                if (copy.TryGetValue(eventType, out Subscription[]? existing))
                {
                    var updated = new Subscription[existing.Length + 1];
                    Array.Copy(existing, updated, existing.Length);
                    updated[^1] = subscription;
                    Array.Sort(updated, SubscriptionComparer.Instance);
                    copy[eventType] = updated;
                }
                else
                {
                    copy[eventType] = new[] { subscription };
                }

                Volatile.Write(ref _snapshot, copy);
            }

            return new SubscriptionToken(this, eventType, sequence);
        }

        internal void RemoveSubscription(Type eventType, long id)
        {
            lock (_writeLock)
            {
                if (!_snapshot.TryGetValue(eventType, out Subscription[]? existing))
                    return;

                int index = -1;
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i].Id == id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                    return;

                var copy = new Dictionary<Type, Subscription[]>(_snapshot);
                if (existing.Length == 1)
                {
                    copy.Remove(eventType);
                }
                else
                {
                    var updated = new Subscription[existing.Length - 1];
                    Array.Copy(existing, 0, updated, 0, index);
                    Array.Copy(existing, index + 1, updated, index, existing.Length - index - 1);
                    copy[eventType] = updated;
                }

                Volatile.Write(ref _snapshot, copy);
            }
        }

        private int RemoveWhere(Type eventType, Func<Subscription, bool> predicate)
        {
            lock (_writeLock)
            {
                if (!_snapshot.TryGetValue(eventType, out Subscription[]? existing))
                    return 0;

                var remaining = new List<Subscription>(existing.Length);
                int removed = 0;
                foreach (Subscription subscription in existing)
                {
                    if (predicate(subscription))
                        removed++;
                    else
                        remaining.Add(subscription);
                }

                if (removed == 0)
                    return 0;

                var copy = new Dictionary<Type, Subscription[]>(_snapshot);
                if (remaining.Count == 0)
                    copy.Remove(eventType);
                else
                    copy[eventType] = remaining.ToArray();

                Volatile.Write(ref _snapshot, copy);
                return removed;
            }
        }

        private void EnsureDispatchable<TEvent>()
            where TEvent : IEvent
        {
            Type eventType = typeof(TEvent);

            if (eventType == typeof(IEvent))
                throw new InvalidOperationException("不能直接订阅 IEvent；请订阅具体事件类型或基类。");

            if (eventType.IsInterface && !_options.IncludeInterfaces)
                throw new InvalidOperationException(
                    $"事件接口 {eventType.FullName} 需要 EventBusOptions.IncludeInterfaces = true 才能订阅。");

            if (eventType.IsAbstract && !eventType.IsInterface && !_options.IncludeBaseTypes)
                throw new InvalidOperationException(
                    $"抽象事件 {eventType.FullName} 需要 EventBusOptions.IncludeBaseTypes = true 才能订阅。");
        }

        private static string GetDelegateName(Delegate handler)
        {
            MethodInfo method = handler.Method;
            string typeName = method.DeclaringType?.Name ?? "委托";
            return $"{typeName}.{method.Name}";
        }

        private static void EnsureNotAsyncVoid(Delegate handler)
        {
            MethodInfo method = handler.Method;
            if (method.ReturnType == typeof(void) &&
                method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
            {
                throw new ArgumentException(
                    "不支持 async void 处理器；请改用返回 Task 的回调。",
                    nameof(handler));
            }
        }

        #endregion
    }
}
