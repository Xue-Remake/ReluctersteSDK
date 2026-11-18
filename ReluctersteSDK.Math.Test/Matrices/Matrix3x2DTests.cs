using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Matrices;

public class Matrix3x2DTests
{
    [Fact]
    public void Translation_Moves_Points()
    {
        Matrix3x2D m = Matrix3x2D.CreateTranslation(new CartesianDoubleVector2D(3, 4));
        CartesianDoubleVector2D p = m.TransformPoint(new CartesianDoubleVector2D(1, 2));

        Assert.Equal(4.0, p.X, 9);
        Assert.Equal(6.0, p.Y, 9);
    }

    [Fact]
    public void Multiplication_Composes_In_Order()
    {
        Matrix3x2D scale = Matrix3x2D.CreateScale(new CartesianDoubleVector2D(2, 2));
        Matrix3x2D translation = Matrix3x2D.CreateTranslation(new CartesianDoubleVector2D(10, 0));

        CartesianDoubleVector2D result = (scale * translation).TransformPoint(new CartesianDoubleVector2D(1, 0));

        Assert.Equal(12.0, result.X, 9);
        Assert.Equal(0.0, result.Y, 9);
    }

    [Fact]
    public void Inverse_Roundtrips()
    {
        Matrix3x2D m = Matrix3x2D.CreateTRS(
            new CartesianDoubleVector2D(5, -3),
            0.5,
            new CartesianDoubleVector2D(2, 4));

        Assert.True(m.TryInvert(out Matrix3x2D inverse));
        CartesianDoubleVector2D point = new CartesianDoubleVector2D(7, 9);
        CartesianDoubleVector2D back = inverse.TransformPoint(m.TransformPoint(point));

        Assert.True(point.ApproximatelyEquals(back, 1e-9));
    }

    [Fact]
    public void Float_Conversion_Preserves_Values()
    {
        Matrix3x2D m = Matrix3x2D.CreateTranslation(new CartesianDoubleVector2D(1.5, -2.5));
        Matrix3x2D roundtrip = Matrix3x2D.FromFloatMatrix(m.ToFloatMatrix());

        Assert.True(m.ApproximatelyEquals(roundtrip, 1e-6));
    }
}
