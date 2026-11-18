using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 精灵渲染系统：把 (Transform, Sprite) 实体收集成 <see cref="RenderCommand"/> 列表。
    /// <para>
    /// 注册到 <see cref="SystemPhase.Render"/> 阶段；<c>World.Render</c> 之后调用
    /// <see cref="Submit"/> 把命令提交给 <see cref="SpriteBatch"/>。
    /// </para>
    /// </summary>
    public sealed class SpriteRenderSystem : ISystem
    {
        private readonly List<RenderCommand> _commands = new List<RenderCommand>();

        /// <summary>获取上一帧收集到的渲染命令（未排序）。</summary>
        public IReadOnlyList<RenderCommand> Commands => _commands;

        /// <summary>获取或设置摄像机；为 null 时使用世界坐标直接渲染。</summary>
        public Camera2D? Camera { get; set; }

        /// <summary>获取或设置是否使用摄像机可见范围裁剪；默认启用。</summary>
        public bool CullingEnabled { get; set; } = true;

        /// <summary>获取上一帧被裁剪掉的精灵数量。</summary>
        public int LastCulledCount { get; private set; }

        /// <inheritdoc />
        public void Execute(World world, float deltaTime)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));

            _commands.Clear();
            LastCulledCount = 0;

            Camera2D? camera = Camera;
            Matrix3x2 view = camera?.ViewMatrix ?? Matrix3x2.Identity;
            bool cull = camera is not null && CullingEnabled;
            Aabb2D viewBounds = cull ? camera!.ViewBounds : default;

            world.Query<Transform, Sprite>().ForEach((Entity entity, ref Transform transform, ref Sprite sprite) =>
            {
                if (sprite is null || sprite.Texture is null)
                    return;

                // 优先使用 TransformSystem 算好的世界矩阵；没有时退回局部矩阵（扁平场景）
                Matrix3x2 matrix = world.TryGet<WorldTransform>(entity, out WorldTransform worldTransform)
                    ? worldTransform.Value
                    : transform.ToMatrix();

                if (cull)
                {
                    Aabb2D worldBounds = ComputeWorldBounds(sprite, matrix);
                    if (!viewBounds.Intersects(worldBounds))
                    {
                        LastCulledCount++;
                        return;
                    }
                }

                Matrix3x2 screenMatrix = matrix * view;
                Transform2DMath.Decompose(screenMatrix, out Vector2 position, out float rotation, out Vector2 scale);

                _commands.Add(new RenderCommand
                {
                    Texture = sprite.Texture,
                    Source = sprite.Source,
                    Position = position,
                    Rotation = rotation,
                    Scale = scale,
                    Origin = sprite.Origin,
                    Tint = sprite.Tint,
                    Effects = sprite.Effects,
                    Layer = sprite.Layer,
                    Depth = sprite.Depth,
                    EntityIndex = entity.Index
                });
            });
        }

        private static Aabb2D ComputeWorldBounds(Sprite sprite, Matrix3x2 matrix)
        {
            float left = -sprite.Origin.X;
            float top = -sprite.Origin.Y;
            float right = sprite.Source.Width - sprite.Origin.X;
            float bottom = sprite.Source.Height - sprite.Origin.Y;

            Span<CartesianVector2D> corners = stackalloc CartesianVector2D[4];
            corners[0] = matrix.TransformPoint(new CartesianVector2D(left, top));
            corners[1] = matrix.TransformPoint(new CartesianVector2D(right, top));
            corners[2] = matrix.TransformPoint(new CartesianVector2D(right, bottom));
            corners[3] = matrix.TransformPoint(new CartesianVector2D(left, bottom));
            return Aabb2D.FromPoints(corners);
        }

        /// <summary>按 Layer → Depth → 纹理 → 实体索引排序后提交绘制。</summary>
        /// <param name="batch">已 Begin 的精灵批次。</param>
        /// <remarks>
        /// 排序在调用方完成，因此 <c>SpriteBatch.Begin</c> 应当使用 <see cref="SpriteSortMode.Deferred"/>，
        /// 否则 SpriteBatch 会按自己的规则重排，破坏层序。
        /// </remarks>
        public void Submit(SpriteBatch batch)
        {
            if (batch is null) throw new ArgumentNullException(nameof(batch));

            _commands.Sort(Compare);

            for (int i = 0; i < _commands.Count; i++)
            {
                RenderCommand command = _commands[i];
                if (command.Texture is null)
                    continue;

                batch.Draw(
                    command.Texture,
                    command.Position,
                    command.Source,
                    command.Tint,
                    command.Rotation,
                    command.Origin,
                    command.Scale,
                    command.Effects,
                    command.Depth);
            }
        }

        /// <summary>
        /// 渲染命令排序规则：Layer 升序 → Depth 升序 → 纹理 → 实体索引。
        /// </summary>
        /// <param name="a">命令 A。</param>
        /// <param name="b">命令 B。</param>
        /// <returns>负数表示 A 先画。</returns>
        public static int Compare(RenderCommand a, RenderCommand b)
        {
            int result = a.Layer.CompareTo(b.Layer);
            if (result != 0) return result;

            result = a.Depth.CompareTo(b.Depth);
            if (result != 0) return result;

            result = TextureKey(a.Texture).CompareTo(TextureKey(b.Texture));
            if (result != 0) return result;

            return a.EntityIndex.CompareTo(b.EntityIndex);
        }

        private static int TextureKey(Texture2D? texture)
            => texture is null ? 0 : RuntimeHelpers.GetHashCode(texture);
    }
}
