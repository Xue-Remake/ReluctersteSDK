namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>组合令牌；释放幂等，逐个释放内部令牌。</summary>
    internal sealed class CompositeDisposable : IDisposable
    {
        private IDisposable[]? _items;

        public CompositeDisposable(IEnumerable<IDisposable> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            _items = items.ToArray();
        }

        public void Dispose()
        {
            IDisposable[]? items = Interlocked.Exchange(ref _items, null);
            if (items is null)
                return;

            foreach (IDisposable item in items)
                item.Dispose();
        }
    }
}
