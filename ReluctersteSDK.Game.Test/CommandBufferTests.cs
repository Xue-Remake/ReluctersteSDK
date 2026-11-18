using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public class CommandBufferTests
{
    private struct A { }

    [Fact]
    public void Playback_Returns_Command_Count_And_Clears()
    {
        var world = new World();
        var buffer = new CommandBuffer();
        Entity entity = world.Create();

        buffer.Add(entity, new A());
        buffer.Remove<A>(entity);

        Assert.False(buffer.IsEmpty);
        Assert.Equal(2, buffer.Playback(world));
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Destroy_Then_Add_On_Same_Entity_Does_Not_Throw()
    {
        var world = new World();
        var buffer = new CommandBuffer();
        Entity entity = world.Create();

        buffer.Destroy(entity);
        buffer.Add(entity, new A());

        buffer.Playback(world);
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void SetParent_Command_Works()
    {
        var world = new World();
        var buffer = new CommandBuffer();
        Entity parent = world.Create();
        Entity child = world.Create();

        buffer.SetParent(child, parent);
        buffer.Playback(world);

        Assert.Equal(parent, world.GetParent(child));
        Assert.Contains(child, world.GetChildren(parent));
    }

    [Fact]
    public void Exceptions_Are_Aggregated_But_Remaining_Commands_Run()
    {
        var world = new World();
        var buffer = new CommandBuffer();

        buffer.Defer(_ => throw new InvalidOperationException("boom"));
        buffer.Defer(w => w.Create());

        Assert.Throws<AggregateException>(() => buffer.Playback(world));
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void Append_Self_Throws()
    {
        var buffer = new CommandBuffer();
        Assert.Throws<ArgumentException>(() => buffer.Append(buffer));
    }
}
