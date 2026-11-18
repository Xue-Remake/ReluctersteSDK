using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 实体分配器：负责槽位的分配、回收与代数维护。
    /// <para>只管理实体本身；组件数据的清理由 <see cref="World"/> 负责。</para>
    /// </summary>
    public sealed class EntityManager
    {
        private struct Record
        {
            public uint Generation;
            public bool Alive;
        }

        private const int DefaultCapacity = 16;

        private Record[] _records;
        private readonly Stack<int> _freeIndices = new Stack<int>();

        private int _nextIndex;
        private int _aliveCount;
        private bool _freeListDirty;

        /// <summary>使用指定初始容量初始化 <see cref="EntityManager"/>。</summary>
        /// <param name="initialCapacity">初始槽位数量。</param>
        public EntityManager(int initialCapacity = DefaultCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量必须大于 0。");

            _records = new Record[initialCapacity];
        }

        /// <summary>获取当前存活实体数量。</summary>
        public int AliveCount => _aliveCount;

        /// <summary>获取已分配的槽位数量（含已回收的空槽）。</summary>
        public int Capacity => _records.Length;

        /// <summary>获取历史最大槽位索引（用于判断句柄是否越界）。</summary>
        public int MaxIndex => _nextIndex;

        /// <summary>创建一个新实体；优先复用已回收的槽位。</summary>
        /// <returns>新实体的句柄。</returns>
        public Entity Create()
        {
            if (_freeListDirty)
                RebuildFreeList();

            int index;
            uint generation;

            if (_freeIndices.Count > 0)
            {
                index = _freeIndices.Pop();
                generation = ++_records[index].Generation;
                _records[index].Alive = true;
            }
            else
            {
                if (_nextIndex == _records.Length)
                    Array.Resize(ref _records, _records.Length * 2);

                index = _nextIndex++;
                _records[index].Generation = 1;
                _records[index].Alive = true;
                generation = 1;
            }

            _aliveCount++;
            return new Entity(index, generation);
        }

        /// <summary>销毁指定实体。</summary>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>若确实销毁了实体返回 <c>true</c>；句柄已失效时返回 <c>false</c>。</returns>
        public bool Destroy(Entity entity)
        {
            if (!IsAlive(entity))
                return false;

            _records[entity.Index].Alive = false;
            _freeIndices.Push(entity.Index);
            _aliveCount--;
            return true;
        }

        /// <summary>判断句柄是否指向一个当前存活的实体。</summary>
        /// <param name="entity">待检查的实体句柄。</param>
        public bool IsAlive(Entity entity)
        {
            if ((uint)entity.Index >= (uint)_nextIndex)
                return false;

            ref Record record = ref _records[entity.Index];
            return record.Alive && record.Generation == entity.Generation;
        }

        /// <summary>确保至少能容纳指定数量的槽位。</summary>
        /// <param name="capacity">目标容量。</param>
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= _records.Length)
                return;

            int size = _records.Length == 0 ? DefaultCapacity : _records.Length;
            while (size < capacity)
                size *= 2;

            Array.Resize(ref _records, size);
        }

        /// <summary>
        /// 按指定的槽位索引与代数恢复一个存活实体（用于快照恢复）。
        /// <para>不会触发事件，调用方负责重建空闲槽位与组件数据。</para>
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="generation">代数；0 会被替换为 1。</param>
        /// <returns>恢复后的实体句柄。</returns>
        public Entity Restore(int index, uint generation)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "槽位索引不能为负数。");

            EnsureCapacity(index + 1);

            if (_records[index].Alive)
                _aliveCount--;

            _records[index].Generation = generation == 0 ? 1u : generation;
            _records[index].Alive = true;

            if (index >= _nextIndex)
                _nextIndex = index + 1;

            _aliveCount++;
            _freeListDirty = true;
            return new Entity(index, _records[index].Generation);
        }

        /// <summary>获取当前全部存活实体的快照数组。</summary>
        public Entity[] GetAliveEntities()
        {
            var result = new Entity[_aliveCount];
            int count = 0;

            for (int i = 0; i < _nextIndex; i++)
            {
                if (_records[i].Alive)
                    result[count++] = new Entity(i, _records[i].Generation);
            }

            return result;
        }

        /// <summary>清空所有实体（槽位归零，代数重置）。</summary>
        public void Clear()
        {
            Array.Clear(_records, 0, _records.Length);
            _freeIndices.Clear();
            _nextIndex = 0;
            _aliveCount = 0;
            _freeListDirty = false;
        }

        private void RebuildFreeList()
        {
            _freeIndices.Clear();
            for (int i = 0; i < _nextIndex; i++)
            {
                if (!_records[i].Alive)
                    _freeIndices.Push(i);
            }

            _freeListDirty = false;
        }
    }
}
