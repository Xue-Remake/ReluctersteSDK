using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class WorldTests
{
    [Fact]
    public void Add_Then_Get_Returns_Component()
    {
        var world = new World();
        Entity entity = world.Create();

        world.Add(entity, new Position { X = 1, Y = 2 });

        ref Position position = ref world.Get<Position>(entity);
        Assert.Equal(1f, position.X);
        Assert.True(world.Has<Position>(entity));
        Assert.False(world.Has<Velocity>(entity));
    }

    [Fact]
    public void Destroy_Clears_All_Components()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Position());
        world.Add(entity, new Velocity());

        world.Destroy(entity);

        Assert.False(world.IsAlive(entity));
        Assert.False(world.Store<Position>().Contains(entity.Index));
        Assert.False(world.Store<Velocity>().Contains(entity.Index));
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void Add_Null_Reference_Component_Throws()
    {
        var world = new World();
        Entity entity = world.Create();

        Assert.Throws<ArgumentNullException>(() => world.Add<Children>(entity, null!));
    }

    [Fact]
    public void Add_To_Dead_Entity_Throws()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Destroy(entity);

        Assert.Throws<ArgumentException>(() => world.Add(entity, new Position()));
    }

    [Fact]
    public void Query_Returns_Intersection()
    {
        var world = new World();

        Entity positionOnly = world.Create();
        world.Add(positionOnly, new Position());

        Entity both = world.Create();
        world.Add(both, new Position());
        world.Add(both, new Velocity());

        Entity velocityOnly = world.Create();
        world.Add(velocityOnly, new Velocity());

        var seen = new List<Entity>();
        world.Query<Position, Velocity>().ForEach((Entity e, ref Position p, ref Velocity v) => seen.Add(e));

        Assert.Single(seen);
        Assert.Equal(both, seen[0]);
    }

    [Fact]
    public void Query_ForEach_Can_Mutate_Component_By_Reference()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Position { X = 0, Y = 0 });
        world.Add(entity, new Velocity { Value = new CartesianVector2D(2, 3) });

        const float dt = 0.5f;
        world.Query<Position, Velocity>().ForEach((Entity e, ref Position p, ref Velocity v) =>
        {
            p.X += v.Value.X * dt;
            p.Y += v.Value.Y * dt;
        });

        Assert.Equal(1f, world.Get<Position>(entity).X);
        Assert.Equal(1.5f, world.Get<Position>(entity).Y);
    }

    [Fact]
    public void ForEach_Structural_Modification_Throws()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Position());

        Assert.Throws<InvalidOperationException>(() =>
            world.Query<Position>().ForEach((Entity e, ref Position p) => world.Add(e, new Velocity())));
    }

    [Fact]
    public void CommandBuffer_Defers_Destroy()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new Position());

        world.Commands.Destroy(entity);

        Assert.True(world.IsAlive(entity));   // 尚未回放

        world.Commands.Playback(world);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void CommandBuffer_Create_Invokes_OnCreate()
    {
        var world = new World();
        Entity created = Entity.Null;

        world.Commands.Create(e =>
        {
            created = e;
            world.Add(e, new Position { X = 5 });
        });

        world.Commands.Playback(world);

        Assert.True(world.IsAlive(created));
        Assert.Equal(5f, world.Get<Position>(created).X);
    }

    [Fact]
    public void Systems_Execute_In_Registration_Order()
    {
        var world = new World();
        var log = new List<string>();

        world.AddSystem(new RecordingSystem("A", log));
        world.AddSystem(new RecordingSystem("B", log));
        world.AddSystem(new RecordingSystem("C", log));

        world.Update(0.016f);

        Assert.Equal(new[] { "A", "B", "C" }, log);
    }

    [Fact]
    public void Commands_Flush_Between_Systems()
    {
        var world = new World();
        Entity entity = world.Create();
        var observed = new List<bool>();

        world.AddSystem(new DeletingSystem(entity));
        world.AddSystem(new ObserverSystem(entity, observed));

        world.Update(0.016f);

        Assert.Single(observed);
        Assert.False(observed[0]);   // 第二个系统应当已经看不到它
    }

    private sealed class RecordingSystem : ISystem
    {
        private readonly string _name;
        private readonly List<string> _log;

        public RecordingSystem(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public void Execute(World world, float deltaTime) => _log.Add(_name);
    }

    private sealed class DeletingSystem : ISystem
    {
        private readonly Entity _target;

        public DeletingSystem(Entity target) => _target = target;

        public void Execute(World world, float deltaTime) => world.Commands.Destroy(_target);
    }

    private sealed class ObserverSystem : ISystem
    {
        private readonly Entity _target;
        private readonly List<bool> _observed;

        public ObserverSystem(Entity target, List<bool> observed)
        {
            _target = target;
            _observed = observed;
        }

        public void Execute(World world, float deltaTime) => _observed.Add(world.IsAlive(_target));
    }
}
