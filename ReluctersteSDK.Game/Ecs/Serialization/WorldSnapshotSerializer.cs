using System.Text.Json;
using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Ecs.Serialization
{
    /// <summary>
    /// 世界快照的捕获、恢复与 JSON 读写。
    /// <para>只保存已注册的纯数据组件；<see cref="Rendering.Sprite"/>、<see cref="Rendering.Animator"/>
    /// 等引用运行时资源的组件不参与。</para>
    /// </summary>
    public static class WorldSnapshotSerializer
    {
        /// <summary>捕获当前世界状态。</summary>
        /// <param name="world">源世界。</param>
        /// <param name="registry">组件编解码注册表。</param>
        /// <param name="options">可选选项。</param>
        public static WorldSnapshot Capture(
            World world,
            ComponentCodecRegistry registry,
            WorldSnapshotOptions? options = null)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (registry is null) throw new ArgumentNullException(nameof(registry));

            options ??= new WorldSnapshotOptions();
            var snapshot = new WorldSnapshot();

            foreach (Entity entity in world.Entities.GetAliveEntities())
            {
                var entitySnapshot = new EntitySnapshot
                {
                    Index = entity.Index,
                    Generation = entity.Generation
                };

                foreach (Type type in registry.RegisteredTypes)
                {
                    if (!registry.TryGet(type, out IComponentCodec codec) || !codec.Has(world, entity))
                        continue;

                    entitySnapshot.Components.Add(new ComponentSnapshot
                    {
                        TypeName = codec.TypeName,
                        Data = codec.Write(world, entity, options.JsonOptions)
                    });
                }

                snapshot.Entities.Add(entitySnapshot);
            }

            if (options.IncludeSingletons)
            {
                foreach (Type type in registry.RegisteredTypes)
                {
                    if (!registry.TryGet(type, out IComponentCodec codec) || !codec.HasSingleton(world))
                        continue;

                    snapshot.Singletons.Add(new SingletonSnapshot
                    {
                        TypeName = codec.TypeName,
                        Data = codec.WriteSingleton(world, options.JsonOptions)
                    });
                }
            }

            return snapshot;
        }

        /// <summary>把快照恢复到世界。目标世界必须为空，或允许 <see cref="WorldSnapshotOptions.ClearBeforeRestore"/>。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="snapshot">快照。</param>
        /// <param name="registry">组件编解码注册表。</param>
        /// <param name="options">可选选项。</param>
        public static void Restore(
            World world,
            WorldSnapshot snapshot,
            ComponentCodecRegistry registry,
            WorldSnapshotOptions? options = null)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
            if (registry is null) throw new ArgumentNullException(nameof(registry));

            options ??= new WorldSnapshotOptions();

            if (world.EntityCount > 0)
            {
                if (!options.ClearBeforeRestore)
                    throw new InvalidOperationException("目标世界非空；请先清空或启用 ClearBeforeRestore。");

                world.ClearForRestore();
            }

            var entities = new Dictionary<int, Entity>(snapshot.Entities.Count);

            foreach (EntitySnapshot entitySnapshot in snapshot.Entities)
            {
                Entity entity = world.RestoreEntity(entitySnapshot.Index, entitySnapshot.Generation);
                entities[entitySnapshot.Index] = entity;
            }

            foreach (EntitySnapshot entitySnapshot in snapshot.Entities)
            {
                if (!entities.TryGetValue(entitySnapshot.Index, out Entity entity))
                    continue;

                foreach (ComponentSnapshot component in entitySnapshot.Components)
                {
                    if (!registry.TryGet(component.TypeName, out IComponentCodec codec))
                    {
                        if (options.ThrowOnUnsupportedComponent)
                            throw new InvalidOperationException($"未注册的组件类型：{component.TypeName}");

                        continue;
                    }

                    codec.Read(world, entity, component.Data, options.JsonOptions);
                }
            }

            // Children 是派生数据，由 Parent 重建
            foreach (EntitySnapshot entitySnapshot in snapshot.Entities)
            {
                if (!entities.TryGetValue(entitySnapshot.Index, out Entity entity))
                    continue;

                if (world.TryGet<Parent>(entity, out Parent parent)
                    && entities.ContainsValue(parent.Value)
                    && parent.Value != entity)
                {
                    world.SetParent(entity, parent.Value);
                }
            }

            if (options.IncludeSingletons)
            {
                foreach (SingletonSnapshot singleton in snapshot.Singletons)
                {
                    if (!registry.TryGet(singleton.TypeName, out IComponentCodec codec))
                    {
                        if (options.ThrowOnUnsupportedComponent)
                            throw new InvalidOperationException($"未注册的单例类型：{singleton.TypeName}");

                        continue;
                    }

                    codec.ReadSingleton(world, singleton.Data, options.JsonOptions);
                }
            }
        }

        /// <summary>把世界状态写入 JSON 流。</summary>
        public static void Save(
            World world,
            Stream destination,
            ComponentCodecRegistry registry,
            WorldSnapshotOptions? options = null)
        {
            if (destination is null) throw new ArgumentNullException(nameof(destination));

            options ??= new WorldSnapshotOptions();
            WorldSnapshot snapshot = Capture(world, registry, options);
            JsonSerializer.Serialize(destination, snapshot, options.JsonOptions);
        }

        /// <summary>从 JSON 流读取并恢复世界状态。</summary>
        public static void Load(
            World world,
            Stream source,
            ComponentCodecRegistry registry,
            WorldSnapshotOptions? options = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            options ??= new WorldSnapshotOptions();
            WorldSnapshot? snapshot = JsonSerializer.Deserialize<WorldSnapshot>(source, options.JsonOptions);
            if (snapshot is null)
                throw new JsonException("快照 JSON 为空或格式不正确。");

            Restore(world, snapshot, registry, options);
        }
    }
}
