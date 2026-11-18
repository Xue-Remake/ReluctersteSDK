using ReluctersteSDK.Math.Interpolation;

namespace ReluctersteSDK.Math.Test.Interpolation;

public class InterpolationTests
{
    [Theory]
    [InlineData(EasingMode.Linear)]
    [InlineData(EasingMode.QuadIn)]
    [InlineData(EasingMode.QuadOut)]
    [InlineData(EasingMode.CubicInOut)]
    [InlineData(EasingMode.SineInOut)]
    [InlineData(EasingMode.SmoothStep)]
    [InlineData(EasingMode.SmootherStep)]
    public void Easing_Has_Correct_Endpoints(EasingMode mode)
    {
        Assert.Equal(0f, Easing.Evaluate(mode, 0f), 3);
        Assert.Equal(1f, Easing.Evaluate(mode, 1f), 3);
    }

    [Fact]
    public void Easing_Clamps_Input()
    {
        Assert.Equal(0f, Easing.Evaluate(EasingMode.QuadIn, -1f), 3);
        Assert.Equal(1f, Easing.Evaluate(EasingMode.QuadIn, 2f), 3);
    }

    [Fact]
    public void SmoothStep_Midpoint_Is_Half()
    {
        Assert.Equal(0.5f, Easing.SmoothStep(0.5f), 3);
        Assert.Equal(0.5f, Easing.SmootherStep(0.5f), 3);
    }

    [Fact]
    public void Lerp_Clamps_But_Unclamped_Does_Not()
    {
        Assert.Equal(10f, Interp.Lerp(0, 10, 2f), 3);
        Assert.Equal(20f, Interp.LerpUnclamped(0, 10, 2f), 3);
    }

    [Fact]
    public void LerpAngle_Takes_Shortest_Path()
    {
        float result = Interp.LerpAngle(MathF.PI * 1.9f, MathF.PI * 0.1f, 0.5f);
        Assert.Equal(MathF.PI * 2f, result, 2);
    }

    [Fact]
    public void MoveTowards_Does_Not_Overshoot()
    {
        Assert.Equal(3f, Interp.MoveTowards(0f, 10f, 3f), 3);
        Assert.Equal(10f, Interp.MoveTowards(0f, 10f, 20f), 3);
    }

    [Fact]
    public void Hermite_And_CatmullRom_Hit_Endpoints()
    {
        Assert.Equal(2f, Interp.Hermite(2f, 0f, 5f, 0f, 0f), 3);
        Assert.Equal(5f, Interp.Hermite(2f, 0f, 5f, 0f, 1f), 3);
        Assert.Equal(2f, Interp.CatmullRom(1f, 2f, 5f, 6f, 0f), 3);
        Assert.Equal(5f, Interp.CatmullRom(1f, 2f, 5f, 6f, 1f), 3);
    }
}
