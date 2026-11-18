using ReluctersteSDK.Math.QuickRandom;

namespace ReluctersteSDK.Math.Test.QuickRandom;

public class SeededRandomTests
{
    [Fact]
    public void Same_Seed_Produces_Same_Sequence()
    {
        var a = new SeededRandom(12345);
        var b = new SeededRandom(12345);

        for (int i = 0; i < 32; i++)
            Assert.Equal(a.NextUInt32(), b.NextUInt32());
    }

    [Fact]
    public void Different_Seeds_Produce_Different_Sequences()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        bool anyDifferent = false;
        for (int i = 0; i < 8; i++)
            anyDifferent |= a.NextUInt32() != b.NextUInt32();

        Assert.True(anyDifferent);
    }

    [Fact]
    public void NextDouble_Is_In_Range()
    {
        var rng = new SeededRandom(7);
        for (int i = 0; i < 10000; i++)
        {
            double value = rng.NextDouble();
            Assert.InRange(value, 0.0, 0.9999999999999999);
        }
    }

    [Fact]
    public void NextInt_Is_In_Range_And_Excludes_Upper_Bound()
    {
        var rng = new SeededRandom(9);
        for (int i = 0; i < 10000; i++)
        {
            int value = rng.NextInt(-5, 5);
            Assert.InRange(value, -5, 4);
        }
    }

    [Fact]
    public void Fork_Produces_Reproducible_Independent_Stream()
    {
        var first = new SeededRandom(42);
        var second = new SeededRandom(42);

        SeededRandom fork1 = first.Fork();
        SeededRandom fork2 = second.Fork();

        Assert.Equal(fork1.NextUInt32(), fork2.NextUInt32());
        Assert.NotEqual(first.NextUInt32(), fork1.NextUInt32());
    }

    [Fact]
    public void Capture_And_Restore_Continues_Sequence()
    {
        var rng = new SeededRandom(99);
        for (int i = 0; i < 10; i++) rng.NextUInt32();

        RandomState state = rng.CaptureState();
        uint expected = rng.NextUInt32();

        rng.RestoreState(state);
        Assert.Equal(expected, rng.NextUInt32());
    }

    [Fact]
    public void Reseed_Resets_Sequence()
    {
        var rng = new SeededRandom(123);
        uint first = rng.NextUInt32();
        rng.Reseed(123);
        Assert.Equal(first, rng.NextUInt32());
    }

    [Fact]
    public void Fill_Writes_Buffer()
    {
        var rng = new SeededRandom(5);
        var buffer = new byte[16];
        rng.Fill(buffer);

        Assert.Contains(buffer, b => b != 0);
    }
}
