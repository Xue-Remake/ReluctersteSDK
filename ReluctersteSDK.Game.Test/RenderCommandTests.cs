using ReluctersteSDK.Game.Rendering;

namespace ReluctersteSDK.Game.Test;

public class RenderCommandTests
{
    [Fact]
    public void Compare_Puts_Lower_Layer_First()
    {
        var back = new RenderCommand { Layer = 0, EntityIndex = 5 };
        var front = new RenderCommand { Layer = 10, EntityIndex = 0 };

        Assert.True(SpriteRenderSystem.Compare(back, front) < 0);
        Assert.True(SpriteRenderSystem.Compare(front, back) > 0);
    }

    [Fact]
    public void Compare_Uses_Depth_Within_Same_Layer()
    {
        var near = new RenderCommand { Layer = 1, Depth = 0.1f, EntityIndex = 9 };
        var far = new RenderCommand { Layer = 1, Depth = 0.9f, EntityIndex = 0 };

        Assert.True(SpriteRenderSystem.Compare(near, far) < 0);
    }

    [Fact]
    public void Compare_Falls_Back_To_Entity_Index()
    {
        var a = new RenderCommand { Layer = 0, Depth = 0f, EntityIndex = 1 };
        var b = new RenderCommand { Layer = 0, Depth = 0f, EntityIndex = 2 };

        Assert.True(SpriteRenderSystem.Compare(a, b) < 0);
        Assert.Equal(0, SpriteRenderSystem.Compare(a, a));
    }

    [Fact]
    public void Compare_Handles_Null_Textures()
    {
        var a = new RenderCommand { Layer = 0, EntityIndex = 0, Texture = null };
        var b = new RenderCommand { Layer = 0, EntityIndex = 1, Texture = null };

        Assert.True(SpriteRenderSystem.Compare(a, b) < 0);
    }

    [Fact]
    public void Sort_Produces_Layer_Depth_Index_Order()
    {
        var commands = new List<RenderCommand>
        {
            new RenderCommand { Layer = 2, Depth = 0, EntityIndex = 0 },
            new RenderCommand { Layer = 0, Depth = 0.5f, EntityIndex = 1 },
            new RenderCommand { Layer = 0, Depth = 0.1f, EntityIndex = 2 },
            new RenderCommand { Layer = 2, Depth = 0, EntityIndex = 3 },
        };

        commands.Sort(SpriteRenderSystem.Compare);

        Assert.Equal(2, commands[0].EntityIndex);
        Assert.Equal(1, commands[1].EntityIndex);
        Assert.Equal(0, commands[2].EntityIndex);
        Assert.Equal(3, commands[3].EntityIndex);
    }
}
