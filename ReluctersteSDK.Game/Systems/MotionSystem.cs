using System;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Systems
{
    /// <summary>
    /// 运动系统：把 <see cref="Velocity"/> / <see cref="Acceleration"/> / <see cref="Damping"/>
    /// 积分到 <see cref="Transform.LocalPosition"/>。
    /// <para>
    /// 每帧顺序：先更新速度（加速度 → 阻尼 → 限速），再用新速度推进位置；
    /// 若实体带 <see cref="Heading"/>，同时更新朝向。
    /// </para>
    /// </summary>
    public sealed class MotionSystem : ISystem
    {
        private const float EpsilonSqr = 1e-12f;

        /// <inheritdoc />
        public void Execute(World world, float deltaTime)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));

            world.Query<Transform, Velocity>().ForEach((Entity entity, ref Transform transform, ref Velocity velocity) =>
            {
                CartesianVector2D acceleration =
                    world.TryGet<Acceleration>(entity, out Acceleration a) ? a.Value : CartesianVector2D.Zero;

                if (world.TryGet<Damping>(entity, out Damping damping) && damping.Coefficient > 0f)
                {
                    // 速度接近零时归零，避免方向抖动
                    if (velocity.Value.SqrMagnitude < EpsilonSqr)
                        velocity.Value = CartesianVector2D.Zero;

                    velocity.Value += (acceleration - damping.Coefficient * velocity.Value) * deltaTime;

                    if (damping.MaxSpeed > 0f && velocity.Value.SqrMagnitude > damping.MaxSpeed * damping.MaxSpeed)
                        velocity.Value = velocity.Value.Normalized * damping.MaxSpeed;
                }
                else
                {
                    velocity.Value += acceleration * deltaTime;
                }

                transform.LocalPosition += velocity.Value * deltaTime;
                transform.IsDirty = true;

                if (velocity.Value.SqrMagnitude > EpsilonSqr && world.Has<Heading>(entity))
                    world.Get<Heading>(entity).Radians = MathF.Atan2(velocity.Value.Y, velocity.Value.X);
            });
        }
    }
}
