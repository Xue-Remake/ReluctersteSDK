namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>单个处理器的执行结果分类。</summary>
    internal enum HandlerOutcomeKind
    {
        Success,
        Error,
        Canceled,
        TimedOut,
    }

    /// <summary>处理器执行结果；不抛异常，由调用方聚合。</summary>
    internal sealed class HandlerOutcome
    {
        private HandlerOutcome(HandlerOutcomeKind kind, Exception? exception)
        {
            Kind = kind;
            Exception = exception;
        }

        public static readonly HandlerOutcome Success = new(HandlerOutcomeKind.Success, null);
        public static readonly HandlerOutcome Canceled = new(HandlerOutcomeKind.Canceled, null);

        public HandlerOutcomeKind Kind { get; }
        public Exception? Exception { get; }

        public static HandlerOutcome Error(Exception exception)
            => new(HandlerOutcomeKind.Error, exception);

        public static HandlerOutcome Timeout(Exception exception)
            => new(HandlerOutcomeKind.TimedOut, exception);
    }
}
