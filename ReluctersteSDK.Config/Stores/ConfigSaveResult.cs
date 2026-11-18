namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 保存模式。
    /// </summary>
    public enum SaveMode
    {
        /// <summary>仅插入；主键已存在时视为失败。</summary>
        Insert,

        /// <summary>插入或覆盖。</summary>
        Upsert
    }

    /// <summary>
    /// 保存结果。替代原先"失败只返回 false、异常被吞掉"的不可诊断语义。
    /// </summary>
    /// <param name="Success">是否全部写入成功。</param>
    /// <param name="Written">实际写入成功的条数；批量中途失败时用于判断部分写入。</param>
    /// <param name="Error">失败原因；成功时为 null。</param>
    public readonly record struct ConfigSaveResult(bool Success, int Written, Exception? Error)
    {
        /// <summary>构造一个成功结果。</summary>
        /// <param name="written">写入条数。</param>
        public static ConfigSaveResult Ok(int written) => new(true, written, null);

        /// <summary>构造一个失败结果。</summary>
        /// <param name="error">失败原因。</param>
        /// <param name="written">失败前已写入的条数。</param>
        public static ConfigSaveResult Fail(Exception error, int written = 0)
            => new(false, written, error ?? throw new ArgumentNullException(nameof(error)));
    }
}
