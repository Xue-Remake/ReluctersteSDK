using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 用于标记类、结构体或字段为可被日志系统识别的事件特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class LogEventAttribute : Attribute
    {
        /// <summary>获取事件的分类标签。</summary>
        public string Tag { get; }
        /// <summary>
        /// 初始化 <see cref="LogEventAttribute"/> 类的新实例。
        /// </summary>
        /// <param name="tag">输出日志时展示的前缀标签，默认为 <c>"EVENT"</c>。</param>
        public LogEventAttribute(string tag = "EVENT")
        {
            Tag = tag;
        }
    }
}
