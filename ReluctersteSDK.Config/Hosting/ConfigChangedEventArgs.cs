using ReluctersteSDK.Config.Stores;

namespace ReluctersteSDK.Config.Hosting
{
    /// <summary>
    /// 配置变更事件参数。只在保存成功且确实写入条目时触发。
    /// </summary>
    public sealed class ConfigChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建事件参数。
        /// </summary>
        /// <param name="section">配置节名。</param>
        /// <param name="configType">配置类型。</param>
        /// <param name="written">写入条数。</param>
        /// <param name="mode">保存模式。</param>
        public ConfigChangedEventArgs(string section, Type configType, int written, SaveMode mode)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            ConfigType = configType ?? throw new ArgumentNullException(nameof(configType));
            Written = written;
            Mode = mode;
        }

        /// <summary>配置节名（注册时指定，默认是类型名）。</summary>
        public string Section { get; }

        /// <summary>配置类型。</summary>
        public Type ConfigType { get; }

        /// <summary>本次写入的条数。</summary>
        public int Written { get; }

        /// <summary>本次保存模式。</summary>
        public SaveMode Mode { get; }
    }
}
