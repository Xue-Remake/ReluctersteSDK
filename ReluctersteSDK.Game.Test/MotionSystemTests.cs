using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Systems;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class MotionSystemTests
{
    private static World CreateWorld()
    {
        var world = new World();
        world.AddSystem(new MotionSystem());
        return world;
    }

    [Fact]
    public void Velocity_Integrates_Position()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(10, 0) });

        world.Update(0.5f);

        Assert.Equal(5f, world.Get<Transform>(entity).LocalPosition.X, 3);
    }

    [Fact]
    public void Acceleration_Changes_Velocity_Before_Position()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity());
        world.Add(entity, new Acceleration { Value = new CartesianVector2D(10, 0) });

        world.Update(0.5f);

        Assert.Equal(5f, world.Get<Velocity>(entity).Value.X, 3);              // v = 0 + 10*0.5
        Assert.Equal(2.5f, world.Get<Transform>(entity).LocalPosition.X, 3);   // x = 0 + 5*0.5
    }

    [Fact]
    public void Damping_Decays_Velocity()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(10, 0) });
        world.Add(entity, new Damping { Coefficient = 1f, MaxSpeed = 0f });

        world.Update(0.1f);

        Assert.Equal(9f, world.Get<Velocity>(entity).Value.X, 3);             // v = 10 - 1*10*0.1
        Assert.Equal(0.9f, world.Get<Transform>(entity).LocalPosition.X, 3);   // x = 0 + 9*0.1
    }

    [Fact]
    public void MaxSpeed_Clamps_Velocity()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(100, 0) });
        world.Add(entity, new Damping { Coefficient = 0.01f, MaxSpeed = 10f });

        world.Update(0.1f);

        Assert.Equal(10f, world.Get<Velocity>(entity).Value.X, 3);
    }

    [Fact]
    public void Zero_MaxSpeed_Means_Unlimited()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(100, 0) });
        world.Add(entity, new Damping { Coefficient = 0.01f, MaxSpeed = 0f });

        world.Update(0.1f);

        Assert.Equal(99.9f, world.Get<Velocity>(entity).Value.X, 3);
    }

    [Fact]
    public void Zero_Coefficient_Disables_Damping()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(10, 0) });
        world.Add(entity, new Damping { Coefficient = 0f, MaxSpeed = 0f });

        world.Update(0.5f);

        Assert.Equal(10f, world.Get<Velocity>(entity).Value.X, 3);
    }

    [Fact]
    public void Heading_Follows_Velocity()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        world.Add(entity, Transform.Identity);
        world.Add(entity, new Velocity { Value = new CartesianVector2D(0, 10) });
        world.Add(entity, new Heading());

        world.Update(0.1f);

        Assert.Equal(MathF.PI / 2, world.Get<Heading>(entity).Radians, 3);
    }

    [Fact]
    public void Entity_Without_Velocity_Is_Ignored()
    {
        World world = CreateWorld();
        Entity entity = world.Create();
        var transform = Transform.Identity;
        transform.LocalPosition = new CartesianVector2D(3, 3);
        world.Add(entity, transform);

        world.Update(0.5f);

        Assert.Equal(3f, world.Get<Transform>(entity).LocalPosition.X, 3);
    }
}
