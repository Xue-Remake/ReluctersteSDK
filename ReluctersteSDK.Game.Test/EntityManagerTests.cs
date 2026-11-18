using ReluctersteSDK.Game.Ecs;

namespace ReluctersteSDK.Game.Test;

public class EntityManagerTests
{
    [Fact]
    public void Create_Returns_Alive_Entity()
    {
        var manager = new EntityManager();

        Entity entity = manager.Create();

        Assert.False(entity.IsNull);
        Assert.True(manager.IsAlive(entity));
        Assert.Equal(1, manager.AliveCount);
    }

    [Fact]
    public void Destroy_Makes_Entity_Not_Alive()
    {
        var manager = new EntityManager();
        Entity entity = manager.Create();

        Assert.True(manager.Destroy(entity));
        Assert.False(manager.IsAlive(entity));
        Assert.Equal(0, manager.AliveCount);
    }

    [Fact]
    public void Reused_Index_Gets_New_Generation()
    {
        var manager = new EntityManager();
        Entity first = manager.Create();
        manager.Destroy(first);

        Entity second = manager.Create();

        Assert.Equal(first.Index, second.Index);
        Assert.NotEqual(first.Generation, second.Generation);
        Assert.False(manager.IsAlive(first));   // 旧句柄必须失效
        Assert.True(manager.IsAlive(second));
    }

    [Fact]
    public void Destroy_Stale_Handle_Returns_False()
    {
        var manager = new EntityManager();
        Entity first = manager.Create();
        manager.Destroy(first);
        manager.Create();   // 复用槽位，代数改变

        Assert.False(manager.Destroy(first));
        Assert.Equal(1, manager.AliveCount);
    }

    [Fact]
    public void Null_Entity_Is_Not_Alive()
    {
        var manager = new EntityManager();

        Assert.False(manager.IsAlive(Entity.Null));
        Assert.True(Entity.Null.IsNull);
    }
}
