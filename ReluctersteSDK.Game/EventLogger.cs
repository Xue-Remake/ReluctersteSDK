using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 提供基于事件特性及带标签的高亮控制台日志记录工具。
    /// </summary>
    public static class EventLogger
    {
        /// <summary>
        /// 检查指定事件对象的类型是否带有 <see cref="LogEventAttribute"/> 特性，若有则格式化输出到控制台。
        /// </summary>
        /// <typeparam name="T">事件的数据类型。</typeparam>
        /// <param name="eventData">事件实例对象（用于反射获取其类型特性）。</param>
        /// <param name="message">要输出的日志内容。</param>
        public static void Emit<T>(T eventData, string message)
        {
            var type = typeof(T);
            var attr = (LogEventAttribute?)Attribute.GetCustomAttribute(type, typeof(LogEventAttribute));

            // 只打印带有 [LogEvent] 标记的事件
            if (attr != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{timestamp}] [{attr.Tag}] ");
                Console.ResetColor();
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// 手动输出带有指定标签和控制台前景色样式的日志信息。
        /// </summary>
        /// <param name="tag">日志前缀标签。</param>
        /// <param name="message">要输出的日志内容。</param>
        /// <param name="color">标签文本的高亮颜色，默认为 <see cref="ConsoleColor.Yellow"/>。</param>
        public static void LogMarked(string tag, string message, ConsoleColor color = ConsoleColor.Yellow)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.ForegroundColor = color;
            Console.Write($"[{timestamp}] [{tag}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}
