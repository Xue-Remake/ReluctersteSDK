using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public class StatsTests
{
    private struct A { }

    private sealed class NoopSystem : ISystem
    {
        public void Execute(World world, float deltaTime) { }
    }

    [Fact]
    public void Stats_Report_Counts_And_Timings()
    {
        var world = new World { StatisticsEnabled = true };
        Entity entity = world.Create();
        world.Add(entity, new A());
        world.AddSystem(new NoopSystem());

        world.Update(0.016f);

        WorldStats stats = world.GetStats();
        Assert.Equal(1, stats.EntityCount);
        Assert.Equal(1, stats.ComponentCount);
        Assert.Equal(1, stats.UpdateSystemCount);
        Assert.Equal(1, stats.FrameCount);
        Assert.True(stats.UpdateMilliseconds >= 0);
        Assert.NotEmpty(world.SystemTimings);
        Assert.Single(world.GetComponentStats());
    }

    [Fact]
    public void Stats_Are_Disabled_By_Default()
    {
        var world = new World();
        world.Update(0.016f);

        Assert.False(world.StatisticsEnabled);
        Assert.Empty(world.SystemTimings);
    }
}
