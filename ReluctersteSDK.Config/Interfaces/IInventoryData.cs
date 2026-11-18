namespace ReluctersteSDK.Config.Interfaces
{
    /// <summary>
    /// 背包/库存数据模型接口。
    /// </summary>
    public interface IInventoryData<TEntity> : IData<TEntity>, IConfig
    {
        /// <summary>背包/库存 ID。</summary>
        int InventoryId { get; }
    }
}
