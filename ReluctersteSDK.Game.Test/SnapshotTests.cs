using System.Numerics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Ecs.Serialization;
using ReluctersteSDK.Game.Systems;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class SnapshotTests
{
    public struct Health
    {
        public int Value;
    }

    [Fact]
    public void Snapshot_RoundTrips_Entities_And_Components()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Transform
        {
            LocalPosition = new CartesianVector2D(3, 4),
            LocalScale = CartesianVector2D.One
        });
        world.Add(entity, new Health { Value = 10 });

        var registry = ComponentCodecRegistry.CreateDefault();
        registry.Register<Health>();

        WorldSnapshot snapshot = WorldSnapshotSerializer.Capture(world, registry);

        var target = new World();
        WorldSnapshotSerializer.Restore(target, snapshot, registry);

        Assert.Equal(1, target.EntityCount);
        Entity restored = target.Entities.GetAliveEntities()[0];
        Assert.Equal(entity.Index, restored.Index);
        Assert.Equal(entity.Generation, restored.Generation);
        Assert.Equal(10, target.Get<Health>(restored).Value);
        Assert.Equal(3f, target.Get<Transform>(restored).LocalPosition.X, 3);
    }

    [Fact]
    public void Snapshot_Preserves_Parent_And_Rebuilds_Children()
    {
        var world = new World();
        Entity parent = world.Create();
        world.Add(parent, new Transform
        {
            LocalPosition = new CartesianVector2D(10, 0),
            LocalScale = CartesianVector2D.One
        });
        Entity child = world.Create();
        world.Add(child, new Transform
        {
            LocalPosition = new CartesianVector2D(5, 0),
            LocalScale = CartesianVector2D.One
        });
        world.SetParent(child, parent);

        var registry = ComponentCodecRegistry.CreateDefault();
        WorldSnapshot snapshot = WorldSnapshotSerializer.Capture(world, registry);

        var target = new World();
        WorldSnapshotSerializer.Restore(target, snapshot, registry);

        Entity restoredParent = target.Entities.GetAliveEntities()[0];
        Entity restoredChild = target.Entities.GetAliveEntities()[1];
        Assert.Equal(restoredParent, target.GetParent(restoredChild));
        Assert.Contains(restoredChild, target.GetChildren(restoredParent));

        new TransformSystem().Execute(target, 0.016f);
        Vector2 position = Vector2.Transform(Vector2.Zero, target.Get<WorldTransform>(restoredChild).Value);
        Assert.Equal(15f, position.X, 3);
    }

    [Fact]
    public void Snapshot_Can_Be_Saved_And_Loaded_From_Stream()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Health { Value = 99 });

        var registry = ComponentCodecRegistry.CreateDefault();
        registry.Register<Health>();

        using var stream = new MemoryStream();
        WorldSnapshotSerializer.Save(world, stream, registry);
        stream.Position = 0;

        var target = new World();
        WorldSnapshotSerializer.Load(target, stream, registry);

        Assert.Equal(99, target.Get<Health>(target.Entities.GetAliveEntities()[0]).Value);
    }

    [Fact]
    public void Unregistered_Component_Is_Skipped_When_Configured()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Health { Value = 5 });

        var registry = ComponentCodecRegistry.CreateDefault();
        var options = new WorldSnapshotOptions { ThrowOnUnsupportedComponent = false };
        WorldSnapshot snapshot = WorldSnapshotSerializer.Capture(world, registry, options);

        var target = new World();
        WorldSnapshotSerializer.Restore(target, snapshot, registry, options);

        Assert.Equal(1, target.EntityCount);
        Assert.False(target.Has<Health>(target.Entities.GetAliveEntities()[0]));
    }
}
