namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>收集一次分发中的处理器错误，并按订阅顺序稳定排列。</summary>
    internal sealed class DispatchErrorCollector
    {
        private readonly List<(Subscription Subscription, EventHandlerError Error)> _items = new();

        public void Add(Subscription subscription, EventHandlerError error)
            => _items.Add((subscription, error));

        public IReadOnlyList<EventHandlerError> ToOrderedList()
            => _items
                .OrderBy(item => item.Subscription.Order)
                .ThenBy(item => item.Subscription.Sequence)
                .Select(item => item.Error)
                .ToList();
    }
}
