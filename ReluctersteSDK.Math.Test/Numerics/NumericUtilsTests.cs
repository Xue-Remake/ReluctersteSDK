using ReluctersteSDK.Math.Numerics;

namespace ReluctersteSDK.Math.Test.Numerics;

public class NumericUtilsTests
{
    [Fact]
    public void Clamp_And_Lerp_Work()
    {
        Assert.Equal(5f, NumericUtils.Clamp(10f, 0f, 5f), 3);
        Assert.Equal(1f, NumericUtils.Clamp01(2f), 3);
        Assert.Equal(5f, NumericUtils.Lerp(0f, 10f, 0.5f), 3);
        Assert.Equal(0.5f, NumericUtils.InverseLerp(0f, 10f, 5f), 3);
    }

    [Fact]
    public void Remap_Maps_Intervals()
    {
        Assert.Equal(50f, NumericUtils.Remap(5f, 0f, 10f, 0f, 100f), 3);
    }

    [Fact]
    public void Repeat_And_PingPong_Work()
    {
        Assert.Equal(1f, NumericUtils.Repeat(-9f, 10f), 3);
        Assert.Equal(9f, NumericUtils.PingPong(9f, 10f), 3);
        Assert.Equal(9f, NumericUtils.PingPong(11f, 10f), 3);
    }

    [Fact]
    public void Angle_Helpers_Work()
    {
        Assert.Equal(MathF.PI / 2, NumericUtils.WrapAngle(MathF.PI * 2.5f), 3);
        Assert.Equal(0.2f * MathF.PI, NumericUtils.DeltaAngle(MathF.PI * 1.9f, MathF.PI * 0.1f), 3);
        Assert.Equal(MathF.PI / 2, NumericUtils.WrapAngleDegrees(450f) * MathF.PI / 180f, 3);
    }

    [Fact]
    public void PowerOfTwo_Helpers_Work()
    {
        Assert.Equal(16, NumericUtils.NextPowerOfTwo(9));
        Assert.Equal(1, NumericUtils.NextPowerOfTwo(0));
        Assert.True(NumericUtils.IsPowerOfTwo(16));
        Assert.False(NumericUtils.IsPowerOfTwo(18));
    }

    [Fact]
    public void ApproximatelyEquals_Respects_Epsilon()
    {
        Assert.True(NumericUtils.ApproximatelyEquals(1f, 1.0000001f));
        Assert.False(NumericUtils.ApproximatelyEquals(1f, 1.01f));
    }
}
