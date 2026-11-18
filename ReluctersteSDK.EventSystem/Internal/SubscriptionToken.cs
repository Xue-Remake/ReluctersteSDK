namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>精确退订单个订阅的令牌；释放幂等。</summary>
    internal sealed class SubscriptionToken : IDisposable
    {
        private EventBus? _bus;
        private readonly Type _eventType;
        private readonly long _id;

        public SubscriptionToken(EventBus bus, Type eventType, long id)
        {
            _bus = bus;
            _eventType = eventType;
            _id = id;
        }

        public void Dispose()
        {
            EventBus? bus = Interlocked.Exchange(ref _bus, null);
            bus?.RemoveSubscription(_eventType, _id);
        }
    }
}
