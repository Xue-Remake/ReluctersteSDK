using System;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 组件存储的非泛型契约，供 <see cref="World"/> 以统一方式级联清理。
    /// </summary>
    public interface IComponentStore
    {
        /// <summary>获取该存储所持有的组件类型。</summary>
        Type ComponentType { get; }

        /// <summary>获取当前组件数量。</summary>
        int Count { get; }

        /// <summary>判断指定实体槽位是否持有本类型组件。</summary>
        /// <param name="entityIndex">实体槽位索引。</param>
        bool Contains(int entityIndex);

        /// <summary>按实体槽位移除组件。</summary>
        /// <param name="entityIndex">实体槽位索引。</param>
        /// <returns>若确实移除了组件返回 <c>true</c>。</returns>
        bool Remove(int entityIndex);

        /// <summary>清空所有组件。</summary>
        void Clear();
    }
}
