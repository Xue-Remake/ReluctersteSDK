using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Vectors;

public class Vector2IntTests
{
    [Fact]
    public void Operators_Work()
    {
        var a = new Vector2Int(3, 4);
        var b = new Vector2Int(1, 2);

        Assert.Equal(new Vector2Int(4, 6), a + b);
        Assert.Equal(new Vector2Int(2, 2), a - b);
        Assert.Equal(new Vector2Int(6, 8), a * 2);
        Assert.Equal(new Vector2Int(1, 2), a / 2);
        Assert.Equal(new Vector2Int(-3, -4), -a);
    }

    [Fact]
    public void FromCartesian_Rounds_By_Default()
    {
        Assert.Equal(new Vector2Int(1, -2), Vector2Int.FromCartesian(new CartesianVector2D(1.4f, -2.4f)));
        Assert.Equal(new Vector2Int(2, -3), Vector2Int.FromCartesian(new CartesianVector2D(1.5f, -2.6f)));
    }

    [Fact]
    public void Grid_Metrics_Work()
    {
        var v = new Vector2Int(-3, 4);

        Assert.Equal(7, v.LengthManhattan);
        Assert.Equal(25, v.SqrMagnitude);
        Assert.Equal(new CartesianVector2D(-3, 4), v.ToCartesianVector2D());
    }
}
