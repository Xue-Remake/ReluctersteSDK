namespace ReluctersteSDK.Config.Interfaces
{
    /// <summary>
    /// 物品实体接口。
    /// </summary>
    public interface IItemEntity<TSelf, TData> : IEntity<TSelf, TData>
        where TSelf : IItemEntity<TSelf, TData>
        where TData : IItemData<TSelf>
    {
        /// <summary>物品名称。</summary>
        string Name { get; }
        /// <summary>物品 ID。</summary>
        int ItemId { get; }
    }
}
