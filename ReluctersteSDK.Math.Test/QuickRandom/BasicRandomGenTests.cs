using ReluctersteSDK.Math.QuickRandom;

namespace ReluctersteSDK.Math.Test.QuickRandom;

public class BasicRandomGenTests
{
    [Fact]
    public void Generic_Next_Respects_Inclusive_Bounds()
    {
        for (int i = 0; i < 1000; i++)
            Assert.InRange(BasicRandomGen.Next(1, 5), 1, 5);
    }

    [Fact]
    public void Fast_Overloads_Match_Documented_Ranges()
    {
        for (int i = 0; i < 1000; i++)
        {
            Assert.InRange(BasicRandomGen.NextInt(0, 5), 0, 4);
            Assert.InRange(BasicRandomGen.NextFloat(0f, 1f), 0f, 1f);
            Assert.InRange(BasicRandomGen.NextDouble(-1.0, 1.0), -1.0, 1.0);
        }
    }

    [Fact]
    public void Reseed_Makes_Sequence_Reproducible()
    {
        BasicRandomGen.Reseed(12345);
        int first = BasicRandomGen.Next(0, 1000000);

        BasicRandomGen.Reseed(12345);
        int second = BasicRandomGen.Next(0, 1000000);

        Assert.Equal(first, second);
    }

    [Fact]
    public void NextBool_Is_Reasonably_Balanced()
    {
        int trueCount = 0;
        const int n = 10000;

        for (int i = 0; i < n; i++)
            if (BasicRandomGen.NextBool())
                trueCount++;

        Assert.InRange(trueCount / (double)n, 0.47, 0.53);
    }
}
