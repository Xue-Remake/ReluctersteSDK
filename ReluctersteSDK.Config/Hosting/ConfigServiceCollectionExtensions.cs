using Microsoft.Extensions.DependencyInjection;

namespace ReluctersteSDK.Config.Hosting
{
    /// <summary>
    /// 配置模块的依赖注入扩展。
    /// </summary>
    /// <remarks>
    /// 只依赖 <c>Microsoft.Extensions.DependencyInjection.Abstractions</c>，
    /// 不会把整个容器实现拖进使用方。
    /// </remarks>
    public static class ConfigServiceCollectionExtensions
    {
        /// <summary>
        /// 注册一个单例 <see cref="ConfigRegistry"/>，并允许在注册时追加配置节。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configure">可选的注册回调，在里面调用 <see cref="ConfigRegistry.Register{T}"/>。</param>
        /// <returns>同一个 <paramref name="services"/> 实例，便于链式调用。</returns>
        public static IServiceCollection AddReluctersteSDKConfig(
            this IServiceCollection services,
            Action<ConfigRegistry>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var registry = new ConfigRegistry();
            configure?.Invoke(registry);
            services.AddSingleton(registry);
            return services;
        }
    }
}
