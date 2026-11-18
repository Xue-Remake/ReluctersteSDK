using System.Numerics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Systems;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class TransformDirtyTests
{
    private static Vector2 WorldPosition(World world, Entity entity)
        => Vector2.Transform(Vector2.Zero, world.Get<WorldTransform>(entity).Value);

    [Fact]
    public void Idle_Frame_Does_Not_Bump_Structural_Version()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Transform
        {
            LocalPosition = new CartesianVector2D(3, 4),
            LocalScale = CartesianVector2D.One
        });

        var system = new TransformSystem();
        system.Execute(world, 0.016f);
        int version = world.StructuralVersion;

        system.Execute(world, 0.016f);
        Assert.Equal(version, world.StructuralVersion);
    }

    [Fact]
    public void Parent_Move_Propagates_To_Child()
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

        var system = new TransformSystem();
        system.Execute(world, 0.016f);
        Assert.Equal(15f, WorldPosition(world, child).X, 3);

        world.Get<Transform>(parent).LocalPosition = new CartesianVector2D(20, 0);
        system.Execute(world, 0.016f);
        Assert.Equal(25f, WorldPosition(world, child).X, 3);
    }

    [Fact]
    public void MarkDirty_Forces_Recompute()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Transform { LocalScale = CartesianVector2D.One });

        var system = new TransformSystem();
        system.Execute(world, 0.016f);
        int version = world.StructuralVersion;

        world.Get<Transform>(entity).IsDirty = true;
        system.Execute(world, 0.016f);

        Assert.Equal(version, world.StructuralVersion);
    }

    [Fact]
    public void FullRebuild_Does_Not_Throw()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);

        var system = new TransformSystem();
        system.Execute(world, 0.016f);
        system.FullRebuild();
        system.Execute(world, 0.016f);

        Assert.True(world.Has<WorldTransform>(entity));
    }
}
