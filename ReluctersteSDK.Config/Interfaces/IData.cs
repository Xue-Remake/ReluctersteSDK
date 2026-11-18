namespace ReluctersteSDK.Config.Interfaces
{
    /// <summary>
    /// 数据模型接口，可转换为对应的实体。
    /// </summary>
    public interface IData<TEntity>
    {
        /// <summary>转换为实体。</summary>
        TEntity ToEntity();
    }
}
