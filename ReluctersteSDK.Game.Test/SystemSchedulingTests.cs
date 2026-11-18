using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public class SystemSchedulingTests
{
    private sealed class RecordingSystem : ISystem
    {
        private readonly List<string> _log;
        private readonly string _name;

        public RecordingSystem(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public int StartCount { get; private set; }
        public int StopCount { get; private set; }

        public void Execute(World world, float deltaTime) => _log.Add(_name);

        public void OnStart(World world)
        {
            StartCount++;
            _log.Add("start:" + _name);
        }

        public void OnStop(World world)
        {
            StopCount++;
            _log.Add("stop:" + _name);
        }
    }

    [Fact]
    public void Phases_Execute_In_Order()
    {
        var world = new World();
        var log = new List<string>();

        world.AddSystem(new RecordingSystem("startup", log), SystemPhase.Startup);
        world.AddSystem(new RecordingSystem("update", log), SystemPhase.Update);
        world.AddSystem(new RecordingSystem("late", log), SystemPhase.LateUpdate);
        world.AddSystem(new RecordingSystem("render", log), SystemPhase.Render);
        world.AddSystem(new RecordingSystem("teardown", log), SystemPhase.Teardown);

        world.Start();
        world.Update(0.016f);
        world.Render(0.016f);
        world.Shutdown();

        Assert.Equal(
            new[]
            {
                "start:startup", "start:update", "start:late", "start:render", "start:teardown",
                "startup",
                "update", "late",
                "render",
                "teardown",
                "stop:startup", "stop:update", "stop:late", "stop:render", "stop:teardown"
            },
            log);
    }

    [Fact]
    public void Priority_And_Registration_Order_Are_Stable()
    {
        var world = new World();
        var log = new List<string>();

        world.AddSystem(new RecordingSystem("default1", log));
        world.AddSystem(new RecordingSystem("late", log), SystemPhase.Update, 10);
        world.AddSystem(new RecordingSystem("early", log), SystemPhase.Update, -10);
        world.AddSystem(new RecordingSystem("default2", log));

        world.Start();
        log.Clear();
        world.Update(0.016f);

        Assert.Equal(new[] { "early", "default1", "default2", "late" }, log);
    }

    [Fact]
    public void Disabled_System_Is_Skipped()
    {
        var world = new World();
        var log = new List<string>();
        var system = new RecordingSystem("system", log);

        world.AddSystem(system);
        world.SetSystemEnabled(system, false);
        world.Update(0.016f);

        Assert.Empty(log);
        Assert.False(world.IsSystemEnabled(system));

        world.SetSystemEnabled(system, true);
        world.Update(0.016f);
        Assert.Equal(new[] { "system" }, log);
    }

    [Fact]
    public void AddSystem_After_Start_Calls_OnStart()
    {
        var world = new World();
        world.Update(0.016f);

        var log = new List<string>();
        var system = new RecordingSystem("late", log);
        world.AddSystem(system);

        Assert.Equal(1, system.StartCount);
    }

    [Fact]
    public void RemoveSystem_Calls_OnStop()
    {
        var world = new World();
        var log = new List<string>();
        var system = new RecordingSystem("system", log);
        world.AddSystem(system);
        world.Start();

        world.RemoveSystem(system);

        Assert.Equal(1, system.StopCount);
        Assert.DoesNotContain(system, world.UpdateSystems);
    }
}
