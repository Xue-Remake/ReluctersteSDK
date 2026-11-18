using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public struct Position
{
    public float X;
    public float Y;
}

public struct Health
{
    public int Value;
}

public class ComponentStoreTests
{
    [Fact]
    public void Add_Then_TryGet_Returns_Value()
    {
        var store = new ComponentStore<Position>();
        var entity = new Entity(0, 1);

        store.Add(entity, new Position { X = 3, Y = 4 });

        Assert.True(store.TryGet(entity, out Position position));
        Assert.Equal(3f, position.X);
        Assert.Equal(4f, position.Y);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Add_Twice_Overwrites_Without_Growing()
    {
        var store = new ComponentStore<Position>();
        var entity = new Entity(0, 1);

        store.Add(entity, new Position { X = 1 });
        store.Add(entity, new Position { X = 9 });

        Assert.Equal(1, store.Count);
        Assert.Equal(9f, store.Get(entity).X);
    }

    [Fact]
    public void Get_Returns_Reference_That_Writes_Through()
    {
        var store = new ComponentStore<Position>();
        var entity = new Entity(0, 1);
        store.Add(entity, new Position { X = 1 });

        ref Position position = ref store.Get(entity);
        position.X = 7;

        Assert.True(store.TryGet(entity, out Position read));
        Assert.Equal(7f, read.X);
    }

    [Fact]
    public void Remove_Uses_SwapRemove_And_Keeps_Other_Mappings()
    {
        var store = new ComponentStore<Position>();
        var e0 = new Entity(0, 1);
        var e1 = new Entity(1, 1);
        var e2 = new Entity(2, 1);

        store.Add(e0, new Position { X = 0 });
        store.Add(e1, new Position { X = 1 });
        store.Add(e2, new Position { X = 2 });

        Assert.True(store.Remove(e0));

        Assert.False(store.TryGet(e0, out _));
        Assert.True(store.TryGet(e1, out Position p1));
        Assert.True(store.TryGet(e2, out Position p2));
        Assert.Equal(1f, p1.X);
        Assert.Equal(2f, p2.X);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Remove_Missing_Returns_False()
    {
        var store = new ComponentStore<Position>();

        Assert.False(store.Remove(new Entity(5, 1)));
        Assert.False(store.Remove(5));
    }

    [Fact]
    public void Get_Missing_Throws()
    {
        var store = new ComponentStore<Position>();

        Assert.Throws<KeyNotFoundException>(() => store.Get(new Entity(0, 1)));
    }

    [Fact]
    public void Sparse_Grows_For_Large_Entity_Index()
    {
        var store = new ComponentStore<Health>();
        var entity = new Entity(500, 1);

        store.Add(entity, new Health { Value = 42 });

        Assert.True(store.TryGet(entity, out Health health));
        Assert.Equal(42, health.Value);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Clear_Empties_Store()
    {
        var store = new ComponentStore<Position>();
        var entity = new Entity(0, 1);
        store.Add(entity, new Position { X = 1 });

        store.Clear();

        Assert.Equal(0, store.Count);
        Assert.False(store.Contains(0));
    }
}
