using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 延迟命令缓冲：把"创建/销毁实体、增删组件"延后到安全时机统一执行。
    /// <para>
    /// 用于在遍历过程中需要结构性修改的场景——直接改会破坏遍历，
    /// 而记录成命令后再回放则完全安全。
    /// </para>
    /// </summary>
    public sealed class CommandBuffer
    {
        private readonly List<Action<World>> _actions = new List<Action<World>>();

        /// <summary>获取待执行的命令数量。</summary>
        public int Count => _actions.Count;

        /// <summary>获取缓冲是否为空。</summary>
        public bool IsEmpty => _actions.Count == 0;

        /// <summary>登记一条"创建实体"命令。</summary>
        /// <param name="onCreate">实体创建后的回调，可用于立即挂载组件。</param>
        public void Create(Action<Entity>? onCreate = null)
        {
            _actions.Add(world =>
            {
                Entity entity = world.Create();
                onCreate?.Invoke(entity);
            });
        }

        /// <summary>登记一条"销毁实体"命令；回放时实体已失效则跳过。</summary>
        /// <param name="entity">目标实体。</param>
        public void Destroy(Entity entity) => _actions.Add(world => world.Destroy(entity));

        /// <summary>登记一条"添加组件"命令；回放时实体已失效则跳过。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件数据。</param>
        public void Add<T>(Entity entity, T component) where T : notnull
            => _actions.Add(world =>
            {
                if (world.IsAlive(entity))
                    world.Add(entity, component);
            });

        /// <summary>登记一条"移除组件"命令；回放时实体已失效则跳过。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        public void Remove<T>(Entity entity) where T : notnull
            => _actions.Add(world =>
            {
                if (world.IsAlive(entity))
                    world.Remove<T>(entity);
            });

        /// <summary>登记一条"设置父节点"命令。</summary>
        public void SetParent(Entity child, Entity parent)
            => _actions.Add(world => world.SetParent(child, parent));

        /// <summary>登记一条"移除父节点"命令。</summary>
        public void RemoveParent(Entity child)
            => _actions.Add(world => world.RemoveParent(child));

        /// <summary>登记一条"设置单例组件"命令。</summary>
        public void SetSingleton<T>(T value) where T : notnull
            => _actions.Add(world => world.SetSingleton(value));

        /// <summary>登记一条"移除单例组件"命令。</summary>
        public void RemoveSingleton<T>() where T : notnull
            => _actions.Add(world => world.RemoveSingleton<T>());

        /// <summary>登记一条自定义命令。</summary>
        /// <param name="action">命令体。</param>
        public void Defer(Action<World> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _actions.Add(action);
        }

        /// <summary>把另一个缓冲中的命令追加到当前缓冲。</summary>
        /// <param name="other">另一个缓冲。</param>
        public void Append(CommandBuffer other)
        {
            if (other is null) throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(other, this))
                throw new ArgumentException("不能把命令缓冲追加到自身。", nameof(other));

            _actions.AddRange(other._actions);
        }

        /// <summary>按登记顺序执行全部命令，并在结束后清空缓冲。</summary>
        /// <param name="world">目标世界。</param>
        /// <returns>本次执行的命令数量。</returns>
        /// <exception cref="AggregateException">至少一条命令抛出异常；其余命令仍会执行。</exception>
        public int Playback(World world)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (_actions.Count == 0)
                return 0;

            // 先清空再执行：允许命令执行过程中再次写入新命令并有确定的行为
            Action<World>[] pending = _actions.ToArray();
            _actions.Clear();

            List<Exception>? errors = null;
            for (int i = 0; i < pending.Length; i++)
            {
                try
                {
                    pending[i](world);
                }
                catch (Exception ex)
                {
                    errors ??= new List<Exception>();
                    errors.Add(ex);
                }
            }

            if (errors is not null && errors.Count > 0)
                throw new AggregateException("命令回放期间发生异常。", errors);

            return pending.Length;
        }

        /// <summary>丢弃全部待执行命令。</summary>
        public void Clear() => _actions.Clear();
    }
}
