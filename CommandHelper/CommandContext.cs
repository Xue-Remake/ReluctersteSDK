using ReluctersteSDK.RegistryKit;

namespace CommandHelper
{
    /// <summary>
    /// 高级命令处理器
    /// </summary>
    /// <param name="RawInput"></param>
    /// <param name="Args"></param>
    /// <param name="Services"></param>
    public record CommandContext(
        string RawInput,
        string[] Args,
        InstanceRegistry Services
        );
}
