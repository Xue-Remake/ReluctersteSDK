using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class EcsFeatureTests
{
    private struct A { public int Value; }
    private struct B { public int Value; }
    private struct C { public int Value; }
    private struct D { public int Value; }

    [Fact]
    public void Query_Three_And_Four_Components_Work()
    {
        var world = new World();
        Entity both = world.Create();
        world.Add(both, new A { Value = 1 });
        world.Add(both, new B { Value = 2 });
        world.Add(both, new C { Value = 3 });
        world.Add(both, new D { Value = 4 });

        Entity partial = world.Create();
        world.Add(partial, new A());
        world.Add(partial, new B());

        Assert.Equal(1, world.Query<A, B, C>().Count);
        Assert.Equal(1, world.Query<A, B, C, D>().Count);

        int sum = 0;
        world.Query<A, B, C, D>().ForEach((Entity e, ref A a, ref B b, ref C c, ref D d) =>
            sum = a.Value + b.Value + c.Value + d.Value);
        Assert.Equal(10, sum);
    }

    [Fact]
    public void Without_Excludes_Entities()
    {
        var world = new World();
        Entity with = world.Create();
        world.Add(with, new A());
        world.Add(with, new B());

        Entity without = world.Create();
        world.Add(without, new A());

        Assert.Equal(1, world.Query<A>().Without<B>().Count);
        Assert.Equal(without, world.Query<A>().Without<B>().ToEntityArray().Single());
    }

    [Fact]
    public void Dead_Entity_Handle_Is_Validated()
    {
        var world = new World();
        Entity entity = world.Create();
        world.Add(entity, new A());
        world.Destroy(entity);

        Assert.False(world.IsAlive(entity));
        Assert.False(world.Has<A>(entity));
        Assert.False(world.TryGet(entity, out A _));
        Assert.False(world.Remove<A>(entity));
        Assert.Throws<ArgumentException>(() => world.Get<A>(entity));
    }

    [Fact]
    public void Reused_Slot_Does_Not_Resurrect_Old_Handle()
    {
        var world = new World();
        Entity old = world.Create();
        world.Destroy(old);
        Entity fresh = world.Create();

        Assert.Equal(old.Index, fresh.Index);
        Assert.NotEqual(old.Generation, fresh.Generation);
        Assert.False(world.IsAlive(old));
    }

    [Fact]
    public void Overwrite_Does_Not_Bump_Structural_Version()
    {
        var world = new World();
        Entity entity = world.Create();

        int beforeAdd = world.StructuralVersion;
        world.Add(entity, new A { Value = 1 });
        int afterAdd = world.StructuralVersion;
        Assert.True(afterAdd > beforeAdd);

        world.Add(entity, new A { Value = 2 });
        Assert.Equal(afterAdd, world.StructuralVersion);
    }

    [Fact]
    public void Direct_Store_Add_Bumps_Structural_Version()
    {
        var world = new World();
        Entity entity = world.Create();
        int before = world.StructuralVersion;

        world.Store<A>().Add(entity, new A());

        Assert.True(world.StructuralVersion > before);
    }

    [Fact]
    public void Singletons_Work()
    {
        var world = new World();
        world.SetSingleton(42);

        Assert.True(world.HasSingleton<int>());
        Assert.Equal(42, world.GetSingleton<int>());
        Assert.True(world.TryGetSingleton(out int value));
        Assert.Equal(42, value);

        world.SetSingleton(7);
        Assert.Equal(7, world.GetSingleton<int>());
        Assert.True(world.RemoveSingleton<int>());
        Assert.False(world.HasSingleton<int>());
    }

    [Fact]
    public void Lifecycle_Events_Fire_In_Order()
    {
        var world = new World();
        var events = new List<string>();

        world.EntityCreated += e => events.Add("created");
        world.EntityDestroyed += e => events.Add("destroyed");
        world.ComponentAdded += e => events.Add("added:" + e.ComponentType.Name);
        world.ComponentRemoved += e => events.Add("removed:" + e.ComponentType.Name);

        Entity entity = world.Create();
        world.Add(entity, new A());
        world.Add(entity, new A()); // 覆盖不触发 added
        world.Destroy(entity);

        Assert.Equal(new[] { "created", "added:A", "removed:A", "destroyed" }, events);
    }

    [Fact]
    public void Clear_Raises_WorldCleared_And_Resets_State()
    {
        var world = new World();
        bool cleared = false;
        world.WorldCleared += () => cleared = true;
        world.Create();
        world.SetSingleton(1);

        world.Clear();

        Assert.True(cleared);
        Assert.Equal(0, world.EntityCount);
        Assert.False(world.HasSingleton<int>());
    }

    [Fact]
    public void Dispose_Is_Idempotent_And_Blocks_Use()
    {
        var world = new World();
        world.Dispose();
        world.Dispose();

        Assert.True(world.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => world.Create());
    }

    [Fact]
    public void Transform_Without_Transform_Child_Does_Not_Throw()
    {
        var world = new World();
        Entity parent = world.Create();
        world.Add(parent, Transform.Identity);
        Entity child = world.Create();
        world.SetParent(child, parent);

        var system = new ReluctersteSDK.Game.Systems.TransformSystem();
        system.Execute(world, 0.016f);

        Assert.True(world.Has<WorldTransform>(parent));
        Assert.False(world.Has<WorldTransform>(child));
    }
}
