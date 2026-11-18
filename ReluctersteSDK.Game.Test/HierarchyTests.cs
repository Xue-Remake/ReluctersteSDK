using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public class HierarchyTests
{
    [Fact]
    public void SetParent_Registers_Both_Sides()
    {
        var world = new World();
        Entity parent = world.Create();
        Entity child = world.Create();

        Assert.True(world.SetParent(child, parent));

        Assert.Equal(parent, world.GetParent(child));
        Assert.Contains(child, world.GetChildren(parent));
    }

    [Fact]
    public void SetParent_Rejects_Self()
    {
        var world = new World();
        Entity entity = world.Create();

        Assert.False(world.SetParent(entity, entity));
        Assert.Equal(Entity.Null, world.GetParent(entity));
    }

    [Fact]
    public void SetParent_Rejects_Cycle()
    {
        var world = new World();
        Entity a = world.Create();
        Entity b = world.Create();

        Assert.True(world.SetParent(b, a));

        Assert.False(world.SetParent(a, b));   // a 的祖先链里有 b，会成环
        Assert.Equal(Entity.Null, world.GetParent(a));
    }

    [Fact]
    public void SetParent_Moves_Between_Parents()
    {
        var world = new World();
        Entity p1 = world.Create();
        Entity p2 = world.Create();
        Entity child = world.Create();

        world.SetParent(child, p1);
        world.SetParent(child, p2);

        Assert.DoesNotContain(child, world.GetChildren(p1));
        Assert.Contains(child, world.GetChildren(p2));
        Assert.Single(world.GetChildren(p2));
    }

    [Fact]
    public void RemoveParent_Detaches_Both_Sides()
    {
        var world = new World();
        Entity parent = world.Create();
        Entity child = world.Create();
        world.SetParent(child, parent);

        Assert.True(world.RemoveParent(child));

        Assert.Equal(Entity.Null, world.GetParent(child));
        Assert.Empty(world.GetChildren(parent));
    }

    [Fact]
    public void Destroy_Parent_Promotes_Children_To_Roots()
    {
        var world = new World();
        Entity parent = world.Create();
        Entity child = world.Create();
        world.SetParent(child, parent);

        world.Destroy(parent);

        Assert.True(world.IsAlive(child));
        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    [Fact]
    public void Destroy_Child_Removes_It_From_Parent_List()
    {
        var world = new World();
        Entity parent = world.Create();
        Entity child = world.Create();
        world.SetParent(child, parent);

        world.Destroy(child);

        Assert.Empty(world.GetChildren(parent));
    }

    [Fact]
    public void IsInAncestorChain_Walks_Up()
    {
        var world = new World();
        Entity a = world.Create();
        Entity b = world.Create();
        Entity c = world.Create();
        world.SetParent(b, a);
        world.SetParent(c, b);

        Assert.True(world.IsInAncestorChain(c, a));
        Assert.True(world.IsInAncestorChain(c, b));
        Assert.False(world.IsInAncestorChain(a, c));
    }

    [Fact]
    public void SetParent_Rejects_Dead_Entities()
    {
        var world = new World();
        Entity parent = world.Create();
        Entity child = world.Create();
        world.Destroy(child);

        Assert.False(world.SetParent(child, parent));
    }
}
