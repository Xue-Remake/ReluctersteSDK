namespace CommandHelper
{
    /// <summary>
    /// 命令特性，用于标记类中的方法以便自动注册为命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string Trigger { get; }
        public string Description { get; }
        public string[] Aliases { get; }
        public CommandAttribute(string trigger, string description, params string[] aliases)
        {
            Trigger = trigger;
            Description = description;
            Aliases = aliases ?? Array.Empty<string>();
        }
    }
}
