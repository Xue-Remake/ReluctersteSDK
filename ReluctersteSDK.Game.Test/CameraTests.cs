using ReluctersteSDK.Game.Rendering;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Test;

public class CameraTests
{
    [Fact]
    public void Identity_Camera_Maps_World_Origin_To_Screen_Center()
    {
        var camera = new Camera2D();
        camera.Resize(800, 600);

        CartesianVector2D screen = camera.WorldToScreen(CartesianVector2D.Zero);

        Assert.Equal(400f, screen.X, 3);
        Assert.Equal(300f, screen.Y, 3);
    }

    [Fact]
    public void Screen_To_World_Roundtrips()
    {
        var camera = new Camera2D
        {
            Position = new CartesianVector2D(10, -5),
            Zoom = 2f,
            Rotation = 0.3f
        };
        camera.Resize(800, 600);

        var world = new CartesianVector2D(25, 7);
        CartesianVector2D back = camera.ScreenToWorld(camera.WorldToScreen(world));

        Assert.True(world.ApproximatelyEquals(back, 1e-4f));
    }

    [Fact]
    public void Zoom_Scales_World_Distance()
    {
        var camera = new Camera2D { Zoom = 2f };
        camera.Resize(800, 600);

        CartesianVector2D screen = camera.WorldToScreen(new CartesianVector2D(100, 0));
        Assert.Equal(600f, screen.X, 3);
        Assert.Equal(300f, screen.Y, 3);
    }

    [Fact]
    public void ViewBounds_Covers_Visible_World()
    {
        var camera = new Camera2D();
        camera.Resize(800, 600);

        var bounds = camera.ViewBounds;
        Assert.True(bounds.Contains(CartesianVector2D.Zero));
        Assert.True(bounds.Contains(new CartesianVector2D(399, 299)));
        Assert.True(bounds.Contains(new CartesianVector2D(-399, -299)));
    }
}
