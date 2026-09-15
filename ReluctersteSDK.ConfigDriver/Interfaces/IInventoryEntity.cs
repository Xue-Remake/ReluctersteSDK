namespace ReluctersteSDK.ConfigDriver.Interfaces
{
    /// <summary>
    /// 背包/库存实体接口。
    /// </summary>
    public interface IInventoryEntity<TSelf, TData> : IEntity<TSelf, TData>
        where TSelf : IInventoryEntity<TSelf, TData>
        where TData : IInventoryData<TSelf>
    {
        /// <summary>背包/库存 ID。/summary>
        int InventoryId { get; }
        #region 查询与预判
        /// <summary>获取指定物品数量。</summary>
        int GetItemCount(int itemId);
        /// <summary>判断是否包含指定数量的物品。</summary>
        bool Contains(int itemId, int count = 1);
        /// <summary>预判能否放入指定数量的物品。</summary>
        bool CanPush(int itemId, int count = 1);
        /// <summary>预判能否取出指定数量的物品。</summary>
        bool CanPop(int itemId, int count = 1);
        #endregion
        #region 核心方法
        /// <summary>放入指定数量的物品。</summary>
        bool Push(int itemId, int count);
        /// <summary>取出指定数量的物品。</summary>
        bool Pop(int itemId, int count);
        /// <summary>尝试放入指定数量的物品，空间不足时放入部分。</summary>
        bool TryPush(int itemId, int count, out int unaddedCount);
        /// <summary>尝试取出指定数量的物品，库存不足时取出部分。</summary>
        bool TryPop(int itemId, int count, out int poppedCount);
        /// <summary>清空背包/库存。</summary>
        void Clear();
        #endregion
    }
}
