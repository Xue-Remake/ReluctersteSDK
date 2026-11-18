using System.Numerics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Systems;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class TransformSystemTests
{
    private static World CreateWorldWithTransformSystem()
    {
        var world = new World();
        world.AddSystem(new TransformSystem());
        return world;
    }

    private static Vector2 WorldPosition(World world, Entity entity)
        => Vector2.Transform(Vector2.Zero, world.Get<WorldTransform>(entity).Value);

    [Fact]
    public void Root_Entity_World_Equals_Local()
    {
        World world = CreateWorldWithTransformSystem();
        Entity entity = world.Create();
        world.Add(entity, new Transform
        {
            LocalPosition = new CartesianVector2D(3, 4),
            LocalScale = CartesianVector2D.One
        });

        world.Update(0.016f);

        Vector2 position = WorldPosition(world, entity);
        Assert.Equal(3f, position.X, 3);
        Assert.Equal(4f, position.Y, 3);
    }

    [Fact]
    public void Child_Accumulates_Parent_Translation()
    {
        World world = CreateWorldWithTransformSystem();
        Entity parent = world.Create();
        Entity child = world.Create();

        world.Add(parent, new Transform { LocalPosition = new CartesianVector2D(10, 0), LocalScale = CartesianVector2D.One });
        world.Add(child, new Transform { LocalPosition = new CartesianVector2D(5, 0), LocalScale = CartesianVector2D.One });
        world.SetParent(child, parent);

        world.Update(0.016f);

        Vector2 position = WorldPosition(world, child);
        Assert.Equal(15f, position.X, 3);
        Assert.Equal(0f, position.Y, 3);
    }

    [Fact]
    public void Child_Accumulates_Parent_Rotation()
    {
        World world = CreateWorldWithTransformSystem();
        Entity parent = world.Create();
        Entity child = world.Create();

        world.Add(parent, new Transform
        {
            LocalPosition = new CartesianVector2D(10, 0),
            LocalRotation = MathF.PI / 2,
            LocalScale = CartesianVector2D.One
        });
        world.Add(child, new Transform
        {
            LocalPosition = new CartesianVector2D(5, 0),
            LocalScale = CartesianVector2D.One
        });
        world.SetParent(child, parent);

        world.Update(0.016f);

        Vector2 position = WorldPosition(world, child);
        Assert.Equal(10f, position.X, 3);
        Assert.Equal(5f, position.Y, 3);
    }

    [Fact]
    public void Child_Accumulates_Parent_Scale()
    {
        World world = CreateWorldWithTransformSystem();
        Entity parent = world.Create();
        Entity child = world.Create();

        world.Add(parent, new Transform { LocalScale = new CartesianVector2D(2, 2) });
        world.Add(child, new Transform { LocalPosition = new CartesianVector2D(5, 0), LocalScale = CartesianVector2D.One });
        world.SetParent(child, parent);

        world.Update(0.016f);

        Vector2 position = WorldPosition(world, child);
        Assert.Equal(10f, position.X, 3);
        Assert.Equal(0f, position.Y, 3);
    }

    [Fact]
    public void Three_Level_Chain_Accumulates()
    {
        World world = CreateWorldWithTransformSystem();
        Entity a = world.Create();
        Entity b = world.Create();
        Entity c = world.Create();

        world.Add(a, new Transform { LocalPosition = new CartesianVector2D(0, 0), LocalScale = CartesianVector2D.One });
        world.Add(b, new Transform { LocalPosition = new CartesianVector2D(10, 0), LocalScale = CartesianVector2D.One });
        world.Add(c, new Transform { LocalPosition = new CartesianVector2D(10, 0), LocalScale = CartesianVector2D.One });
        world.SetParent(b, a);
        world.SetParent(c, b);

        world.Update(0.016f);

        Assert.Equal(20f, WorldPosition(world, c).X, 3);
    }

    [Fact]
    public void Child_Without_Transformed_Parent_Acts_As_Root()
    {
        World world = CreateWorldWithTransformSystem();
        Entity parent = world.Create();          // 只有 Children，没有 Transform
        Entity child = world.Create();

        world.Add(child, new Transform { LocalPosition = new CartesianVector2D(7, 7), LocalScale = CartesianVector2D.One });
        world.SetParent(child, parent);

        world.Update(0.016f);

        Vector2 position = WorldPosition(world, child);
        Assert.Equal(7f, position.X, 3);
        Assert.Equal(7f, position.Y, 3);
    }

    [Fact]
    public void Destroyed_Parent_Leaves_Child_As_Root()
    {
        World world = CreateWorldWithTransformSystem();
        Entity parent = world.Create();
        Entity child = world.Create();

        world.Add(parent, new Transform { LocalPosition = new CartesianVector2D(100, 0), LocalScale = CartesianVector2D.One });
        world.Add(child, new Transform { LocalPosition = new CartesianVector2D(1, 1), LocalScale = CartesianVector2D.One });
        world.SetParent(child, parent);
        world.Update(0.016f);
        Assert.Equal(101f, WorldPosition(world, child).X, 3);

        world.Destroy(parent);
        world.Update(0.016f);

        Vector2 position = WorldPosition(world, child);
        Assert.Equal(1f, position.X, 3);
        Assert.Equal(1f, position.Y, 3);
    }

    [Fact]
    public void Default_Transform_Treats_Zero_Scale_As_One()
    {
        World world = CreateWorldWithTransformSystem();
        Entity entity = world.Create();
        world.Add(entity, new Transform { LocalPosition = new CartesianVector2D(2, 3) });

        world.Update(0.016f);

        // LocalScale 为 (0,0)（default 值）时按 (1,1) 处理，位置不受影响
        Vector2 position = WorldPosition(world, entity);
        Assert.Equal(2f, position.X, 3);
        Assert.Equal(3f, position.Y, 3);
    }
}
