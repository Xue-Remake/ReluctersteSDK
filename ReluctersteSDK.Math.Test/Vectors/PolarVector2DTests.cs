using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Vectors;

public class PolarVector2DTests
{
    [Fact]
    public void Constructor_Normalizes_NegativeRadius_And_Angle()
    {
        var p = new PolarVector2D(-2, -MathF.PI / 2);
        Assert.Equal(2f, p.Radius, 3);
        Assert.True(p.Angle >= 0f && p.Angle < MathF.PI * 2f);
    }

    [Fact]
    public void AngleTo_Returns_Shortest_Signed_Delta()
    {
        var a = new PolarVector2D(1, MathF.PI * 1.9f);
        var b = new PolarVector2D(1, MathF.PI * 0.1f);

        Assert.Equal(0.2f * MathF.PI, a.AngleTo(b), 3);
    }

    [Fact]
    public void LerpUnclamped_Extrapolates()
    {
        var a = new PolarVector2D(1, 0);
        var b = new PolarVector2D(3, MathF.PI / 2);
        var result = PolarVector2D.LerpUnclamped(a, b, 2f);

        Assert.Equal(5f, result.Radius, 3);
        Assert.Equal(MathF.PI, result.Angle, 3);
    }

    [Fact]
    public void Cartesian_Roundtrip_Preserves_Vector()
    {
        var cart = new CartesianVector2D(3, 4);
        var polar = cart.ToPolar();
        var back = polar.ToCartesian();

        Assert.True(cart.ApproximatelyEquals(back, 1e-5f));
    }
}
