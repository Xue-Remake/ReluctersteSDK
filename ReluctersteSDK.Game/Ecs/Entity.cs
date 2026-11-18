using System;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 实体句柄：槽位索引 + 复用代数。
    /// <para>仅 8 字节，可以安全地存进组件、字典与列表；销毁后旧句柄仍然可被识别为失效。</para>
    /// </summary>
    public readonly struct Entity : IEquatable<Entity>
    {
        /// <summary>无效实体句柄。</summary>
        public static readonly Entity Null = new Entity(-1, 0);

        /// <summary>实体槽位索引。</summary>
        public readonly int Index;

        /// <summary>复用代数；同一槽位每次被回收后自增。</summary>
        public readonly uint Generation;

        /// <summary>使用指定槽位与代数初始化实体句柄。</summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="generation">复用代数。</param>
        public Entity(int index, uint generation)
        {
            Index = index;
            Generation = generation;
        }

        /// <summary>获取该句柄是否为无效句柄。</summary>
        public bool IsNull => Index < 0;

        /// <summary>判断两个实体句柄是否指向同一个实体实例。</summary>
        public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Entity other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Index, Generation);

        /// <summary>相等运算符。</summary>
        public static bool operator ==(Entity left, Entity right) => left.Equals(right);

        /// <summary>不等运算符。</summary>
        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => IsNull ? "Entity(Null)" : $"Entity({Index}:{Generation})";
    }
}
