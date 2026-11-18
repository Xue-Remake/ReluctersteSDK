namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 标记一个方法为事件处理器，供 <see cref="EventBus.SubscribeFromTarget"/> 自动扫描注册。
    /// </summary>
    /// <remarks>
    /// 方法必须是实例方法，签名为“一个 <see cref="Interfaces.IEvent"/> 派生参数”
    /// 或“一个事件参数 + <see cref="CancellationToken"/>”，返回 <c>void</c>、<see cref="Task"/>
    /// 或 <see cref="ValueTask"/>。非法签名会在扫描阶段直接抛异常。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EventHandlerAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序；数值越小越先执行。<see cref="int.MinValue"/> 表示未指定，
        /// 此时使用 <see cref="EventBus.SubscribeFromTarget"/> 传入的默认顺序。
        /// </summary>
        public int Order { get; set; } = int.MinValue;
    }
}
