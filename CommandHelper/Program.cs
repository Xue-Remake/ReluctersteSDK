using ReluctersteSDK.RegistryKit;

namespace CommandHelper
{
    public class DataService
    {
        public string GetData() => "核心数据加载成功";
    }
    public class SystemCommands
    {
        [Command("sysinfo", "查看系统信息", "info", "sys")]
        public void ShowSysInfo(CommandContext context)
        {
            Console.WriteLine("系统状态：运行中");
            if (context.Services.TryResolve<DataService>(out var dataService))
            {
                Console.WriteLine($"服务状态： {dataService.GetData()}");
            }
        }
    }
    public class Program
    {
        static async Task Main(string[] args)
        {
            var services = new InstanceRegistry();
            services.RegisterInstance(new DataService());
            var processor = new AdvancedCommandProcessor(services);

            var myCommands = new SystemCommands();
            processor.RegisterFromTarget(myCommands);
            processor.RegisterHelpCommand();
            await processor.ExecuteAsync("sys");

            processor.Disable("sysinfo");
            await processor.ExecuteAsync("info");
        }
    }
}
