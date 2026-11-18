namespace ReluctersteSDK.Config.Interfaces
{
    /// <summary>
    /// 表示一个实体类型，该实体可以转换为对应的数据模型。
    /// </summary>
    public interface IEntity<TSelf, TData>
        where TSelf : IEntity<TSelf, TData>
        where TData : IData<TSelf>
    {
        /// <summary>转换为数据模型。</summary>
        TData ToData();
    }
}
