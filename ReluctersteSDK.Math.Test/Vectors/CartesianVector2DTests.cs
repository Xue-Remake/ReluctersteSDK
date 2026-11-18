using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Vectors;

public class CartesianVector2DTests
{
    [Fact]
    public void Basic_Operators_Work()
    {
        var a = new CartesianVector2D(3, 4);
        var b = new CartesianVector2D(1, 2);

        Assert.Equal(new CartesianVector2D(4, 6), a + b);
        Assert.Equal(new CartesianVector2D(2, 2), a - b);
        Assert.Equal(new CartesianVector2D(6, 8), a * 2);
        Assert.Equal(new CartesianVector2D(1.5f, 2), a / 2);
        Assert.Equal(new CartesianVector2D(-3, -4), -a);
    }

    [Fact]
    public void Magnitude_Dot_Cross_Are_Correct()
    {
        var a = new CartesianVector2D(3, 4);
        var b = new CartesianVector2D(1, 0);

        Assert.Equal(25f, a.SqrMagnitude, 3);
        Assert.Equal(5f, a.Magnitude, 3);
        Assert.Equal(3f, a.Dot(b), 3);
        Assert.Equal(-4f, a.Cross(b), 3);
    }

    [Fact]
    public void Normalized_ZeroVector_Returns_Zero()
    {
        Assert.Equal(CartesianVector2D.Zero, CartesianVector2D.Zero.Normalized);
        Assert.Equal(1f, new CartesianVector2D(3, 4).Normalized.Magnitude, 3);
    }

    [Fact]
    public void Rotate_And_RotateAround_Work()
    {
        var x = new CartesianVector2D(1, 0);
        Assert.True(x.Rotate(MathF.PI / 2).ApproximatelyEquals(new CartesianVector2D(0, 1), 1e-5f));

        var around = new CartesianVector2D(2, 0).RotateAround(new CartesianVector2D(1, 0), MathF.PI);
        Assert.True(around.ApproximatelyEquals(new CartesianVector2D(0, 0), 1e-5f));
    }

    [Fact]
    public void Project_And_Reflect_Work()
    {
        var v = new CartesianVector2D(2, 3);
        var axis = new CartesianVector2D(1, 0);

        Assert.True(v.ProjectOnto(axis).ApproximatelyEquals(new CartesianVector2D(2, 0), 1e-5f));
        Assert.True(v.Reflect(new CartesianVector2D(0, 1)).ApproximatelyEquals(new CartesianVector2D(2, -3), 1e-5f));
    }

    [Fact]
    public void Lerp_Clamps_And_Unclamped_Extrapolates()
    {
        var a = new CartesianVector2D(0, 0);
        var b = new CartesianVector2D(10, 0);

        Assert.Equal(new CartesianVector2D(5, 0), CartesianVector2D.Lerp(a, b, 0.5f));
        Assert.Equal(new CartesianVector2D(10, 0), CartesianVector2D.Lerp(a, b, 2f));
        Assert.Equal(new CartesianVector2D(20, 0), CartesianVector2D.LerpUnclamped(a, b, 2f));
    }

    [Fact]
    public void FromAngle_Uses_Length()
    {
        var v = CartesianVector2D.FromAngle(MathF.PI / 2, 3f);
        Assert.True(v.ApproximatelyEquals(new CartesianVector2D(0, 3), 1e-5f));
    }

    [Fact]
    public void Component_Helpers_Work()
    {
        var v = new CartesianVector2D(-3, 4);

        Assert.Equal(new CartesianVector2D(3, 4), CartesianVector2D.Abs(v));
        Assert.Equal(new CartesianVector2D(-3, 0), CartesianVector2D.Min(v, CartesianVector2D.Zero));
        Assert.Equal(new CartesianVector2D(0, 4), CartesianVector2D.Max(v, CartesianVector2D.Zero));
        Assert.Equal(new CartesianVector2D(0, 3), CartesianVector2D.Clamp(v, CartesianVector2D.Zero, new CartesianVector2D(0, 3)));
        Assert.Equal(new CartesianVector2D(7, 4), v.WithX(7));
        Assert.Equal(new CartesianVector2D(-3, 7), v.WithY(7));
    }

    [Fact]
    public void ClampMagnitude_And_MoveTowards_Work()
    {
        var v = new CartesianVector2D(10, 0);
        Assert.Equal(2f, v.ClampMagnitude(2f).Magnitude, 3);

        var moved = CartesianVector2D.Zero.MoveTowards(new CartesianVector2D(10, 0), 3f);
        Assert.Equal(new CartesianVector2D(3, 0), moved);
        Assert.Equal(new CartesianVector2D(10, 0), CartesianVector2D.Zero.MoveTowards(new CartesianVector2D(10, 0), 20f));
    }

    [Fact]
    public void RotateTowards_Does_Not_Overshoot()
    {
        var start = new CartesianVector2D(1, 0);
        var target = new CartesianVector2D(0, 1);

        var rotated = start.RotateTowards(target, 0.1f);
        Assert.Equal(1f, rotated.Magnitude, 3);
        Assert.Equal(0.1f, start.AngleTo(rotated), 3);

        Assert.Equal(target, start.RotateTowards(target, MathF.PI));
    }
}
