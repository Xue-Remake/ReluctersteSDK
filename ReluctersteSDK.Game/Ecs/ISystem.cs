namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>系统所属的执行阶段。</summary>
    public enum SystemPhase
    {
        /// <summary>启动阶段：只在 <see cref="World.Start"/> 时执行一次。</summary>
        Startup,

        /// <summary>逻辑更新阶段（每帧一次，在渲染之前）。</summary>
        Update,

        /// <summary>逻辑更新后的收尾阶段。</summary>
        LateUpdate,

        /// <summary>渲染准备阶段（产出渲染命令）。</summary>
        Render,

        /// <summary>关闭阶段：只在 <see cref="World.Shutdown"/> 时执行一次。</summary>
        Teardown
    }

    /// <summary>
    /// 系统契约：所有游戏逻辑都以系统为单位实现。
    /// <para>同一阶段内按优先级升序执行，优先级相同按注册顺序。</para>
    /// </summary>
    public interface ISystem
    {
        /// <summary>执行该系统。</summary>
        /// <param name="world">所属世界。</param>
        /// <param name="deltaTime">距上一帧的秒数。</param>
        void Execute(World world, float deltaTime);

        /// <summary>系统被注册到已启动的世界时调用一次。</summary>
        /// <param name="world">所属世界。</param>
        void OnStart(World world) { }

        /// <summary>系统被移除或世界关闭时调用一次。</summary>
        /// <param name="world">所属世界。</param>
        void OnStop(World world) { }
    }
}
