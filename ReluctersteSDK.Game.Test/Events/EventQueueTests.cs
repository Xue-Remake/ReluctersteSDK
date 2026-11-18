using System;
using System.Collections.Generic;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Ecs.Events;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test.Events
{
    /// <summary>
    /// 双缓冲事件队列与 <see cref="WorldEvents"/> 的语义测试。
    /// </summary>
    public class EventQueueTests
    {
        public readonly record struct DamageDealt(Entity Target, int Amount);

        public readonly record struct ItemPicked(int ItemId);

        [Fact]
        public void 同一帧登记后读取为空()
        {
            var queue = new EventQueue<DamageDealt>();
            queue.Raise(new DamageDealt(Entity.Null, 5));

            Assert.Equal(1, queue.PendingCount);
            Assert.Empty(queue.Read.ToArray());
        }

        [Fact]
        public void 交换后可读再交换后清空()
        {
            var queue = new EventQueue<DamageDealt>();
            queue.Raise(new DamageDealt(Entity.Null, 5));
            queue.Swap();

            Assert.Single(queue.Read.ToArray());
            Assert.Equal(5, queue.Read[0].Amount);
            Assert.Equal(0, queue.PendingCount);

            queue.Swap();
            Assert.Empty(queue.Read.ToArray());
        }

        [Fact]
        public void 遍历时登记的事件只出现在下一帧()
        {
            var queue = new EventQueue<DamageDealt>();
            queue.Raise(new DamageDealt(Entity.Null, 1));
            queue.Swap();

            int seen = 0;
            foreach (ref readonly DamageDealt e in queue.Read)
            {
                seen++;
                queue.Raise(new DamageDealt(e.Target, e.Amount + 1));
            }

            Assert.Equal(1, seen);
            Assert.Equal(1, queue.PendingCount);

            queue.Swap();
            Assert.Single(queue.Read.ToArray());
            Assert.Equal(2, queue.Read[0].Amount);
        }

        [Fact]
        public void 清空同时丢弃读写两侧()
        {
            var queue = new EventQueue<DamageDealt>();
            queue.Raise(new DamageDealt(Entity.Null, 1));
            queue.Swap();
            queue.Raise(new DamageDealt(Entity.Null, 2));

            queue.Clear();

            Assert.Empty(queue.Read.ToArray());
            Assert.Equal(0, queue.PendingCount);
        }

        [Fact]
        public void 多类型事件互不干扰()
        {
            var events = new WorldEvents();
            events.Raise(new DamageDealt(Entity.Null, 3));
            events.Raise(new ItemPicked(42));

            Assert.Equal(2, events.PendingCount);
            Assert.Equal(2, events.QueueCount);

            events.Swap();

            Assert.Single(events.Read<DamageDealt>().ToArray());
            Assert.Single(events.Read<ItemPicked>().ToArray());
            Assert.Equal(0, events.PendingCount);
        }

        [Fact]
        public void ReadList与Read内容一致()
        {
            var queue = new EventQueue<DamageDealt>();
            queue.Raise(new DamageDealt(Entity.Null, 1));
            queue.Raise(new DamageDealt(Entity.Null, 2));
            queue.Swap();

            IReadOnlyList<DamageDealt> list = queue.ReadList;

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Amount);
            Assert.Equal(2, list[1].Amount);
        }

        [Fact]
        public void ReadList在下次交换后旧引用被清空()
        {
            var queue = new EventQueue<ItemPicked>();
            queue.Raise(new ItemPicked(1));
            queue.Swap();

            IReadOnlyList<ItemPicked> held = queue.ReadList;
            Assert.Single(held);

            queue.Raise(new ItemPicked(2));
            queue.Swap();

            // 两个列表交替复用：持有的旧引用会被清空，不会抛异常
            Assert.Empty(held);
            Assert.NotSame(held, queue.ReadList);
            Assert.Single(queue.ReadList);
            Assert.Equal(2, queue.ReadList[0].ItemId);
        }

        [Fact]
        public void WorldEventsReadList未登记时返回空列表()
        {
            var events = new WorldEvents();

            Assert.Empty(events.ReadList<ItemPicked>());
            Assert.Equal(0, events.QueueCount);
        }

        [Fact]
        public void WorldEventsReadList与Read等价()
        {
            var events = new WorldEvents();
            events.Raise(new ItemPicked(5));
            events.Swap();

            Assert.Single(events.ReadList<ItemPicked>());
            Assert.Equal(1, events.Read<ItemPicked>().Length);
        }

        [Fact]
        public void 未登记过的类型返回空且不创建队列()
        {
            var events = new WorldEvents();

            Assert.True(events.Read<ItemPicked>().IsEmpty);
            Assert.Equal(0, events.PendingCountOf<ItemPicked>());
            Assert.Equal(0, events.QueueCount);
        }

        [Fact]
        public void 缓存队列句柄与统一登记等价()
        {
            var events = new WorldEvents();
            EventQueue<ItemPicked> queue = events.Queue<ItemPicked>();

            queue.Raise(new ItemPicked(7));
            Assert.Equal(1, events.PendingCountOf<ItemPicked>());

            events.Swap();
            Assert.Equal(7, events.Read<ItemPicked>()[0].ItemId);
        }

        [Fact]
        public void WorldUpdate在开头自动交换()
        {
            using var world = new World();
            var raiser = new OnceRaiseSystem();
            var spy = new RecordingSystem();

            world.AddSystem(raiser);
            world.AddSystem(spy);

            // 第 1 帧：交换先于系统执行，所以同帧登记的事件读不到
            world.Update(0.016f);
            Assert.Empty(spy.Seen);
            Assert.Equal(1, world.Events.PendingCount);

            // 第 2 帧：交换后事件可见
            world.Update(0.016f);
            Assert.Single(spy.Seen);
            Assert.Equal(9, spy.Seen[0].Amount);
            Assert.Equal(0, world.Events.PendingCount);
        }

        [Fact]
        public void Startup阶段登记的事件在首帧可读()
        {
            using var world = new World();
            var startup = new StartupRaiseSystem();
            var spy = new RecordingSystem();

            world.AddSystem(startup, SystemPhase.Startup);
            world.AddSystem(spy);

            world.Update(0.016f);

            Assert.Single(spy.Seen);
        }

        [Fact]
        public void WorldClear清空事件()
        {
            using var world = new World();
            world.Events.Raise(new DamageDealt(Entity.Null, 1));
            world.Events.Swap();

            world.Clear();

            Assert.True(world.Events.Read<DamageDealt>().IsEmpty);
            Assert.Equal(0, world.Events.PendingCount);
        }

        [Fact]
        public void 统计包含事件队列信息()
        {
            using var world = new World();
            world.Events.Raise(new DamageDealt(Entity.Null, 1));

            WorldStats stats = world.GetStats();

            Assert.Equal(1, stats.EventQueueCount);
            Assert.Equal(1, stats.PendingEventCount);
        }

        [Fact]
        public void 消费者在遍历中修改实体结构不抛异常()
        {
            using var world = new World();
            Entity target = world.Create();
            world.Add(target, Transform.Identity);

            var killer = new StructuralMutationSystem();
            world.AddSystem(killer);
            world.Update(0.016f);   // 先跑一帧，让世界进入已启动状态

            world.Events.Raise(new DamageDealt(target, 100));
            world.Update(0.016f);   // 交换后消费，并在遍历中销毁实体

            Assert.Equal(1, killer.Consumed);
            Assert.False(world.IsAlive(target));
        }

        private sealed class RecordingSystem : ISystem
        {
            public List<DamageDealt> Seen { get; } = new List<DamageDealt>();

            public void Execute(World world, float deltaTime)
            {
                foreach (ref readonly DamageDealt e in world.Events.Read<DamageDealt>())
                    Seen.Add(e);
            }
        }

        private sealed class OnceRaiseSystem : ISystem
        {
            private bool _raised;

            public void Execute(World world, float deltaTime)
            {
                if (_raised)
                    return;

                _raised = true;
                world.Events.Raise(new DamageDealt(Entity.Null, 9));
            }
        }

        private sealed class StartupRaiseSystem : ISystem
        {
            public void Execute(World world, float deltaTime)
                => world.Events.Raise(new DamageDealt(Entity.Null, 1));
        }

        private sealed class StructuralMutationSystem : ISystem
        {
            public int Consumed { get; private set; }

            public void Execute(World world, float deltaTime)
            {
                foreach (ref readonly DamageDealt e in world.Events.Read<DamageDealt>())
                {
                    Consumed++;
                    world.Destroy(e.Target);
                }
            }
        }
    }
}
