namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>组件新增/移除事件参数。</summary>
    public readonly struct ComponentEventArgs
    {
        /// <summary>初始化事件参数。</summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="componentType">组件类型。</param>
        public ComponentEventArgs(Entity entity, Type componentType)
        {
            Entity = entity;
            ComponentType = componentType;
        }

        /// <summary>目标实体。</summary>
        public Entity Entity { get; }

        /// <summary>组件类型。</summary>
        public Type ComponentType { get; }
    }
}
