using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.QuickRandom;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.QuickRandom;

public class DistributionSamplerTests
{
    [Fact]
    public void Same_Seed_Sampler_Is_Reproducible()
    {
        var a = new DistributionSampler(new SeededRandom(123));
        var b = new DistributionSampler(new SeededRandom(123));

        for (int i = 0; i < 16; i++)
            Assert.Equal(a.NextNormal(), b.NextNormal(), 12);
    }

    [Fact]
    public void Normal_Distribution_Has_Expected_Mean_And_StdDev()
    {
        var sampler = new DistributionSampler(new SeededRandom(42));
        const int n = 100_000;
        double sum = 0;
        double sumSquares = 0;

        for (int i = 0; i < n; i++)
        {
            double value = sampler.NextNormal();
            sum += value;
            sumSquares += value * value;
        }

        double mean = sum / n;
        double variance = sumSquares / n - mean * mean;

        Assert.InRange(mean, -0.02, 0.02);
        Assert.InRange(variance, 0.95, 1.05);
    }

    [Fact]
    public void Uniform_Distribution_Stays_In_Range()
    {
        var sampler = new DistributionSampler(new SeededRandom(7));
        for (int i = 0; i < 10000; i++)
            Assert.InRange(sampler.NextUniform(-2, 3), -2.0, 3.0);
    }

    [Fact]
    public void Bernoulli_Frequency_Is_Reasonable()
    {
        var sampler = new DistributionSampler(new SeededRandom(11));
        int successes = 0;
        const int n = 100_000;

        for (int i = 0; i < n; i++)
            successes += sampler.NextBernoulli(0.3);

        Assert.InRange(successes / (double)n, 0.29, 0.31);
    }

    [Fact]
    public void Poisson_Mean_Is_Reasonable()
    {
        var sampler = new DistributionSampler(new SeededRandom(13));
        int total = 0;
        const int n = 50_000;

        for (int i = 0; i < n; i++)
            total += sampler.NextPoisson(4);

        Assert.InRange(total / (double)n, 3.9, 4.1);
    }

    [Fact]
    public void Weighted_Pick_Respects_Weights()
    {
        var rng = new SeededRandom(17);
        var items = new[] { "a", "b" };
        var weights = new[] { 0.9, 0.1 };
        int countA = 0;

        for (int i = 0; i < 10000; i++)
            if (rng.PickWeighted(items, weights) == "a")
                countA++;

        Assert.InRange(countA / 10000.0, 0.88, 0.92);
    }

    [Fact]
    public void Shuffle_Is_Deterministic_For_Same_Seed()
    {
        var a = new[] { 1, 2, 3, 4, 5 };
        var b = new[] { 1, 2, 3, 4, 5 };

        new SeededRandom(21).Shuffle(a);
        new SeededRandom(21).Shuffle(b);

        Assert.Equal(a, b);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, a.OrderBy(x => x));
    }

    [Fact]
    public void PointInCircle_Stays_Inside()
    {
        var rng = new SeededRandom(23);
        var circle = new Circle2D(new CartesianVector2D(5, -2), 3f);

        for (int i = 0; i < 10000; i++)
            Assert.True(circle.Contains(rng.NextPointInCircle(circle)));
    }
}
