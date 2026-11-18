using System.Numerics;
using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Matrices;

public class Matrix3x2ExtensionsTests
{
    [Fact]
    public void CreateTRS_And_Decompose_Roundtrip()
    {
        var position = new CartesianVector2D(10, 20);
        const float rotation = 0.75f;
        var scale = new CartesianVector2D(2, 3);

        Matrix3x2 matrix = Matrix3x2Extensions.CreateTRS(position, rotation, scale);
        matrix.Decompose(out CartesianVector2D p, out float r, out CartesianVector2D s);

        Assert.True(position.ApproximatelyEquals(p, 1e-4f));
        Assert.Equal(rotation, r, 3);
        Assert.True(scale.ApproximatelyEquals(s, 1e-4f));
    }

    [Fact]
    public void TransformPoint_Applies_Translation()
    {
        Matrix3x2 matrix = Matrix3x2Extensions.CreateTranslation(new CartesianVector2D(5, 7));
        Assert.True(matrix.TransformPoint(new CartesianVector2D(1, 2))
            .ApproximatelyEquals(new CartesianVector2D(6, 9), 1e-5f));
    }

    [Fact]
    public void TryInvert_Returns_False_For_Singular_Matrix()
    {
        var singular = new Matrix3x2(0, 0, 0, 0, 0, 0);
        Assert.False(singular.TryInvert(out _));
        Assert.Equal(Matrix3x2.Identity, singular.InvertOrIdentity());
    }

    [Fact]
    public void Lerp_Interpolates_Components()
    {
        var a = Matrix3x2.Identity;
        var b = Matrix3x2Extensions.CreateTranslation(new CartesianVector2D(10, 20));
        Matrix3x2 mid = Matrix3x2Extensions.Lerp(a, b, 0.5f);

        Assert.Equal(5f, mid.M31, 3);
        Assert.Equal(10f, mid.M32, 3);
    }
}
