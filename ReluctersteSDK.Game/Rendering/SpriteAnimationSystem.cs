using System;
using Microsoft.Xna.Framework;
using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 帧动画系统：推进每个 <see cref="Animator"/> 的时间，并把当前帧写入 <see cref="Sprite.Source"/>。
    /// </summary>
    /// <remarks>
    /// 执行顺序要求：必须在 <see cref="SpriteRenderSystem"/> **之前**。
    /// 通常是 Update 阶段最后一个逻辑系统，或 Render 阶段注册在渲染系统之前的系统。
    /// </remarks>
    public sealed class SpriteAnimationSystem : ISystem
    {
        /// <inheritdoc />
        public void Execute(World world, float deltaTime)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));

            world.Query<Sprite, Animator>().ForEach((Entity entity, ref Sprite sprite, ref Animator animator) =>
            {
                animator.Tick(deltaTime);

                Rectangle? source = animator.CurrentSource;
                if (source.HasValue)
                    sprite.Source = source.Value;
            });
        }
    }
}
