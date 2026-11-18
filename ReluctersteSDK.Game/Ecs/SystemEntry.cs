namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>系统中一项的只读快照。</summary>
    public readonly struct SystemEntry
    {
        internal SystemEntry(ISystem system, SystemPhase phase, int priority, bool enabled, int registrationOrder)
        {
            System = system;
            Phase = phase;
            Priority = priority;
            Enabled = enabled;
            RegistrationOrder = registrationOrder;
        }

        /// <summary>系统实例。</summary>
        public ISystem System { get; }

        /// <summary>所属阶段。</summary>
        public SystemPhase Phase { get; }

        /// <summary>优先级；数值小的先执行。</summary>
        public int Priority { get; }

        /// <summary>是否启用。</summary>
        public bool Enabled { get; }

        /// <summary>注册顺序。</summary>
        public int RegistrationOrder { get; }
    }

    internal sealed class SystemRegistration
    {
        public SystemRegistration(ISystem system, SystemPhase phase, int priority, bool enabled, int registrationOrder)
        {
            System = system;
            Phase = phase;
            Priority = priority;
            Enabled = enabled;
            RegistrationOrder = registrationOrder;
        }

        public ISystem System { get; }
        public SystemPhase Phase { get; set; }
        public int Priority { get; set; }
        public bool Enabled { get; set; }
        public int RegistrationOrder { get; }

        public SystemEntry ToEntry() => new SystemEntry(System, Phase, Priority, Enabled, RegistrationOrder);
    }
}
