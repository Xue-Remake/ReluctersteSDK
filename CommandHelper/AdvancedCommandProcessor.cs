using ReluctersteSDK.RegistryKit;
using System.Reflection;

namespace CommandHelper
{
    /// <summary>
    /// 高级命令处理器，基于 RegistryKit 构建。
    /// 支持线程安全、别名联动状态管理、依赖注入(DI)以及基于特性的自动注册。
    /// </summary>
    public class AdvancedCommandProcessor
    {
        public delegate Task CommandHandler(CommandContext context);
        /// <summary>
        /// 内部命令定义
        /// </summary>
        public class CommandDefinition
        {
            public string Trigger { get; }
            public string Description { get; }
            public CommandHandler Handler { get; }
            public string[] Aliases { get; }
            public CommandDefinition(string trigger, string description, CommandHandler handler, string[] aliases)
            {
                Trigger = trigger;
                Description = description;
                Handler = handler;
                Aliases = aliases ?? Array.Empty<string>();
            }
        }
        // 使用 ValueRegistry 替代普通的 Dictionary，获得并发安全和原生启用/禁用支持
        private readonly ValueRegistry<string, CommandDefinition> _commandRegistry;

        // 服务注册表，用于命令执行时的依赖注入
        public InstanceRegistry Services { get; }
        public Func<string, Task>? OnUnknownCommand { get; set; }
        public AdvancedCommandProcessor(InstanceRegistry? services = null)
        {
            _commandRegistry = new ValueRegistry<string, CommandDefinition>();
            Services = services ?? new InstanceRegistry();
        }
        /// <summary>
        /// 注册一个异步命令
        /// </summary>
        public AdvancedCommandProcessor Register(string trigger, string description, CommandHandler handler, params string[] aliases)
        {
            var definition = new CommandDefinition(trigger, description, handler, aliases);
            // 注册主触发词
            _commandRegistry.Register(trigger.ToLower(), definition);
            // 注册别名
            foreach (var alias in aliases)
            {
                _commandRegistry.Register(alias.ToLower(), definition);
            }
            return this;
        }
        /// <summary>
        /// 注册一个同步命令
        /// </summary>
        public AdvancedCommandProcessor Register(string trigger, string description, Action<CommandContext> handler, params string[] aliases)
        {
            return Register(trigger, description, context =>
            {
                handler(context);
                return Task.CompletedTask;
            }, aliases);
        }
        /// <summary>
        /// 扫描目标对象，自动注册带有 [Command] 特性的方法。
        /// 方法签名必须为 void Method(CommandContext) 或 Task Method(CommandContext)
        /// </summary>
        public AdvancedCommandProcessor RegisterFromTarget(object target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CommandAttribute>();
                if (attr == null) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandContext))
                {
                    throw new InvalidOperationException($"方法 {method.Name} 必须只包含一个 CommandContext 类型的参数。");
                }
                CommandHandler handler = context =>
                {
                    var result = method.Invoke(target, new object[] { context });
                    if (result is Task task)
                        return task;
                    return Task.CompletedTask;
                };
                Register(attr.Trigger, attr.Description, handler, attr.Aliases);
            }
            return this;
        }
        /// <summary>
        /// 禁用指令（主键或别名均可）。联动禁用其所有关联键。
        /// </summary>
        public bool Disable(string triggerOrAlias)
        {
            string key = triggerOrAlias.ToLower();
            if (_commandRegistry.TryGetValue(key, out var command)) // 这里利用了 ValueRegistry
            {
                _commandRegistry.Disable(command.Trigger.ToLower());
                foreach (var alias in command.Aliases)
                {
                    _commandRegistry.Disable(alias.ToLower());
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// 启用指令（主键或别名均可）。联动启用其所有关联键。
        /// </summary>
        public bool Enable(string triggerOrAlias)
        {
            string key = triggerOrAlias.ToLower();
            // 注意：因为 TryGetValue 默认忽略禁用的条目，这里我们需要用 Exists 并手动提取来重新启用
            // 这里我们采用一种遍历所有键值来查找关联 CommandDefinition 的方式
            if (_commandRegistry.TryGetValue(cmd => string.Equals(cmd.Trigger, key, StringComparison.OrdinalIgnoreCase) ||
                                                    cmd.Aliases.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase)),
                                             out var commands) || _commandRegistry.Exists(key))
            {
                // 即使被禁用，我们通过重新注册（或底层扩展）来激活。
                // 为了兼容 ValueRegistry API，我们可以直接强制 Enable 主键和别名
                // 因为 Exists 判断了存在，但我们需要获取完整定义，最稳妥的是在外部维持一个缓存，
                // 这里的折中方案：调用方需要确切知道主键或别名进行 Enable
                _commandRegistry.Enable(key);

                // 启用后就能拿到了
                if (_commandRegistry.TryGetValue(key, out var command))
                {
                    _commandRegistry.Enable(command.Trigger.ToLower());
                    foreach (var alias in command.Aliases)
                        _commandRegistry.Enable(alias.ToLower());
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 卸载/注销指令（会同时删除主键和所有别名）
        /// </summary>
        public bool Unregister(string triggerOrAlias)
        {
            string key = triggerOrAlias.ToLower();
            if (_commandRegistry.TryGetValue(key, out var command))
            {
                _commandRegistry.Remove(command.Trigger.ToLower());
                foreach (var alias in command.Aliases)
                {
                    _commandRegistry.Remove(alias.ToLower());
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// 注册内置帮助命令
        /// </summary>
        public AdvancedCommandProcessor RegisterHelpCommand()
        {
            return Register("help", "显示所有可用命令", context =>
            {
                Console.WriteLine("\n=== 可用命令列表 ===");

                // 利用 Registry 获取所有启用的命令
                if (_commandRegistry.TryGetValue(_ => true, out var allCommands))
                {
                    // 去重处理
                    var uniqueCommands = allCommands.Distinct().OrderBy(c => c.Trigger);
                    foreach (var cmd in uniqueCommands)
                    {
                        string aliasText = cmd.Aliases.Length > 0 ? $" ({string.Join(", ", cmd.Aliases)})" : "";
                        Console.WriteLine($"{cmd.Trigger,-15}{aliasText,-15} : {cmd.Description}");
                    }
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

            string commandName = parts[0].ToLower();
            string[] args = parts[1..];
            // Registry 自动过滤了已禁用的指令
            if (_commandRegistry.TryGetValue(commandName, out var command))
            {
                try
                {
                    // 将 Services 传入上下文，供命令内部调用
                    var context = new CommandContext(rawInput, args, Services);
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
                // 如果在 Registry 中存在但被禁用了（Exists返回true但TryGetValue返回false）
                if (_commandRegistry.Exists(commandName))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[提示] 指令 '{commandName}' 当前已被禁用。");
                    Console.ResetColor();
                    return;
                }
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
    }
}
