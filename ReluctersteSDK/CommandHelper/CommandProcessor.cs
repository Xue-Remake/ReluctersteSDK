namespace ReluctersteSDK.CommandHelper
{
    /// <summary>
    /// 命令处理器上下文
    /// </summary>
    public record CommandContext(string RawInput, string[] Args);
    /// <summary>
    /// 通用命令处理器
    /// </summary>
    public class CommandProcessor
    {
        public delegate Task CommandHandler(CommandContext context);
        // 内部存储结构：触发词(包含别名) -> 命令定义
        private readonly Dictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);

        // 未知命令的回调
        public Func<string, Task>? OnUnknownCommand { get; set; }
        /// <summary>
        /// 注册一个异步命令
        /// </summary>
        public CommandProcessor Register(string trigger, string description, CommandHandler handler, params string[] aliases)
        {
            var definition = new CommandDefinition(trigger, description, handler, aliases);

            // 注册主触发词
            _commands[trigger] = definition;

            // 注册别名
            foreach (var alias in aliases)
            {
                _commands[alias] = definition;
            }
            return this;
        }
        /// <summary>
        /// 注册一个同步命令
        /// </summary>
        public CommandProcessor Register(string trigger, string description, Action<CommandContext> handler, params string[] aliases)
        {
            return Register(trigger, description, context =>
            {
                handler(context);
                return Task.CompletedTask;
            }, aliases);
        }
        /// <summary>
        /// 1. 禁用指令（主键或别名均可）
        /// </summary>
        public bool Disable(string triggerOrAlias)
        {
            if (_commands.TryGetValue(triggerOrAlias, out var command))
            {
                command.IsEnabled = false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 2. 启用指令（主键或别名均可）
        /// </summary>
        public bool Enable(string triggerOrAlias)
        {
            if (_commands.TryGetValue(triggerOrAlias, out var command))
            {
                command.IsEnabled = true;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 3. 卸载/注销指令（主键或别名均可，会同时删除主键和所有别名）
        /// </summary>
        public bool Unregister(string triggerOrAlias)
        {
            if (_commands.TryGetValue(triggerOrAlias, out var command))
            {
                // 移除主触发词
                _commands.Remove(command.Trigger);

                // 移除所有别名
                foreach (var alias in command.Aliases)
                {
                    _commands.Remove(alias);
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// 自动注册内置的 "help" 命令
        /// </summary>
        public CommandProcessor RegisterHelpCommand()
        {
            return Register("help", "显示所有可用命令", _ =>
            {
                Console.WriteLine("\n=== 可用命令列表 ===");
                // 去重，防止别名重复显示
                var uniqueCommands = _commands.Values.Distinct().OrderBy(c => c.Trigger);
                foreach (var cmd in uniqueCommands)
                {
                    string aliasText = cmd.Aliases.Length > 0 ? $" ({string.Join(", ", cmd.Aliases)})" : "";
                    string statusText = cmd.IsEnabled ? "" : " [已禁用]";

                    if (!cmd.IsEnabled) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"{cmd.Trigger,-15}{aliasText,-15} : {cmd.Description}{statusText}");
                    Console.ResetColor();
                }
                Console.WriteLine("====================\n");
            }, "?", "h");
        }
        /// <summary>
        /// 执行输入的指令
        /// </summary>
        public async Task ExecuteAsync(string? rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return;
            var parts = rawInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;
            string commandName = parts[0];
            string[] args = parts[1..];
            if (_commands.TryGetValue(commandName, out var command))
            {
                // 检查命令是否被禁用
                if (!command.IsEnabled)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[提示] 指令 '{commandName}' 当前已被禁用。");
                    Console.ResetColor();
                    return;
                }
                try
                {
                    var context = new CommandContext(rawInput, args);
                    await command.Handler(context);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Error] 执行命令 '{commandName}' 时出错: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                if (OnUnknownCommand != null)
                {
                    await OnUnknownCommand(commandName);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Warning] 未知指令: '{commandName}'。");
                    Console.ResetColor();
                }
            }
        }
        /// <summary>
        /// 内部命令定义类（改为 Class 以维护可变状态 IsEnabled）
        /// </summary>
        private class CommandDefinition
        {
            public string Trigger { get; }
            public string Description { get; }
            public CommandHandler Handler { get; }
            public string[] Aliases { get; }
            public bool IsEnabled { get; set; } = true; // 默认启用
            public CommandDefinition(string trigger, string description, CommandHandler handler, string[] aliases)
            {
                Trigger = trigger;
                Description = description;
                Handler = handler;
                Aliases = aliases ?? Array.Empty<string>();
            }
        }
    }
}
