using System;
using System.Numerics;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 局部变换组件：相对父节点的位置、旋转（弧度）与缩放。
    /// <para>仅用于 2D；世界矩阵由 <c>TransformSystem</c> 计算并写入 <see cref="WorldTransform"/>。</para>
    /// </summary>
    public struct Transform
    {
        /// <summary>相对父节点的位置。</summary>
        public CartesianVector2D LocalPosition;

        /// <summary>相对父节点的旋转角（弧度，正值为逆时针）。</summary>
        public float LocalRotation;

        /// <summary>相对父节点的缩放。</summary>
        public CartesianVector2D LocalScale;

        /// <summary>提示 TransformSystem 需要重算；直接修改字段时建议置为 true。</summary>
        public bool IsDirty;

        /// <summary>获取单位变换（位置为零、旋转为零、缩放为一）。</summary>
        public static Transform Identity => new Transform
        {
            LocalPosition = CartesianVector2D.Zero,
            LocalRotation = 0f,
            LocalScale = CartesianVector2D.One,
            IsDirty = false
        };

        /// <summary>
        /// 转换为 2D 局部矩阵（先缩放、再旋转、最后平移；行向量约定）。
        /// </summary>
        /// <remarks>
        /// <c>default(Transform)</c> 的 <see cref="LocalScale"/> 是 (0,0)，直接使用会让物体"消失"。
        /// 为避免这个陷阱，缩放为零时按 (1,1) 处理；确实需要零缩放请改用渲染层的可见性开关。
        /// </remarks>
        public Matrix3x2 ToMatrix()
        {
            CartesianVector2D scale = LocalScale.SqrMagnitude > 0f ? LocalScale : CartesianVector2D.One;

            float cos = MathF.Cos(LocalRotation);
            float sin = MathF.Sin(LocalRotation);

            return new Matrix3x2(
                scale.X * cos, scale.X * sin,   // M11, M12
                -scale.Y * sin, scale.Y * cos,  // M21, M22
                LocalPosition.X, LocalPosition.Y); // M31, M32
        }
    }
}
