namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>订阅来源类型，用于退订时区分匹配方式。</summary>
    internal enum SubscriptionKind
    {
        /// <summary>泛型处理器类型订阅。</summary>
        Type,

        /// <summary>已有处理器实例订阅。</summary>
        Instance,

        /// <summary>委托订阅。</summary>
        Delegate,

        /// <summary>特性扫描注册。</summary>
        Attribute,
    }

    /// <summary>不可变订阅记录。</summary>
    internal sealed class Subscription
    {
        public Subscription(
            long id,
            Type eventType,
            SubscriptionKind kind,
            Type? handlerType,
            string handlerName,
            Delegate? originalHandler,
            object? handlerInstance,
            int order,
            long sequence,
            Func<object, CancellationToken, Task<HandlerOutcome>> invoke)
        {
            Id = id;
            EventType = eventType;
            Kind = kind;
            HandlerType = handlerType;
            HandlerName = handlerName;
            OriginalHandler = originalHandler;
            HandlerInstance = handlerInstance;
            Order = order;
            Sequence = sequence;
            Invoke = invoke;
        }

        public long Id { get; }
        public Type EventType { get; }
        public SubscriptionKind Kind { get; }
        public Type? HandlerType { get; }
        public string HandlerName { get; }
        public Delegate? OriginalHandler { get; }
        public object? HandlerInstance { get; }
        public int Order { get; }
        public long Sequence { get; }
        public Func<object, CancellationToken, Task<HandlerOutcome>> Invoke { get; }
    }

    /// <summary>按 Order 升序、再按订阅顺序升序比较。</summary>
    internal sealed class SubscriptionComparer : IComparer<Subscription>
    {
        public static readonly SubscriptionComparer Instance = new();

        public int Compare(Subscription? x, Subscription? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            int order = x.Order.CompareTo(y.Order);
            return order != 0 ? order : x.Sequence.CompareTo(y.Sequence);
        }
    }
}
