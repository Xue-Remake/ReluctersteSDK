using ReluctersteSDK.Math.Noise;

namespace ReluctersteSDK.Math.Test.Noise;

public class NoiseTests
{
    [Fact]
    public void Perlin_Is_Deterministic_For_Same_Seed()
    {
        var a = new PerlinNoise2D(123);
        var b = new PerlinNoise2D(123);

        for (int i = 0; i < 100; i++)
        {
            float x = i * 0.37f;
            float y = i * -0.21f;
            Assert.Equal(a.Sample(x, y), b.Sample(x, y), 6);
        }
    }

    [Fact]
    public void Perlin_Output_Is_In_Reasonable_Range()
    {
        var noise = new PerlinNoise2D(7);
        for (int i = 0; i < 1000; i++)
            Assert.InRange(noise.Sample(i * 0.13f, i * 0.29f), -1.1f, 1.1f);
    }

    [Fact]
    public void Perlin_Different_Seeds_Differ()
    {
        var a = new PerlinNoise2D(1);
        var b = new PerlinNoise2D(2);

        bool anyDifferent = false;
        for (int i = 0; i < 20; i++)
            anyDifferent |= a.Sample(i * 0.5f, i * 0.5f) != b.Sample(i * 0.5f, i * 0.5f);

        Assert.True(anyDifferent);
    }

    [Fact]
    public void Fbm_Stays_In_Range()
    {
        var noise = new PerlinNoise2D(11);
        for (int i = 0; i < 1000; i++)
            Assert.InRange(noise.Fbm(i * 0.1f, i * 0.2f), -1.1f, 1.1f);
    }

    [Fact]
    public void ValueNoise_Is_In_Unit_Range_And_Deterministic()
    {
        var a = new ValueNoise2D(5);
        var b = new ValueNoise2D(5);

        for (int i = 0; i < 1000; i++)
        {
            float value = a.Sample(i * 0.17f, i * 0.11f);
            Assert.InRange(value, 0f, 1f);
            Assert.Equal(value, b.Sample(i * 0.17f, i * 0.11f), 6);
        }
    }
}
