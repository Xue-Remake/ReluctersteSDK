using System.Numerics;
using ReluctersteSDK.Math.Matrices;

namespace ReluctersteSDK.Math.Test.Matrices;

public class Transform2DMathTests
{
    [Fact]
    public void Decompose_Reads_Translation()
    {
        Matrix3x2 matrix = Transform2DMath.Compose(new Vector2(7, -3), 0f, Vector2.One);

        Transform2DMath.Decompose(matrix, out Vector2 position, out float rotation, out Vector2 scale);

        Assert.Equal(7f, position.X, 3);
        Assert.Equal(-3f, position.Y, 3);
        Assert.Equal(0f, rotation, 3);
        Assert.Equal(1f, scale.X, 3);
    }

    [Fact]
    public void Decompose_Reads_Rotation()
    {
        Matrix3x2 matrix = Transform2DMath.Compose(Vector2.Zero, MathF.PI / 2, Vector2.One);

        Transform2DMath.Decompose(matrix, out _, out float rotation, out _);

        Assert.Equal(MathF.PI / 2, rotation, 3);
    }

    [Fact]
    public void Decompose_Reads_Scale()
    {
        Matrix3x2 matrix = Transform2DMath.Compose(Vector2.Zero, 0f, new Vector2(2, 3));

        Transform2DMath.Decompose(matrix, out _, out _, out Vector2 scale);

        Assert.Equal(2f, scale.X, 3);
        Assert.Equal(3f, scale.Y, 3);
    }

    [Fact]
    public void Compose_Then_Decompose_Roundtrips()
    {
        var position = new Vector2(10, 20);
        const float rotation = 0.75f;
        var scale = new Vector2(1.5f, 0.5f);

        Matrix3x2 matrix = Transform2DMath.Compose(position, rotation, scale);
        Transform2DMath.Decompose(matrix, out Vector2 p, out float r, out Vector2 s);

        Assert.Equal(position.X, p.X, 3);
        Assert.Equal(position.Y, p.Y, 3);
        Assert.Equal(rotation, r, 3);
        Assert.Equal(scale.X, s.X, 3);
        Assert.Equal(scale.Y, s.Y, 3);
    }

    [Fact]
    public void Compose_Applies_Scale_Before_Rotation_Before_Translation()
    {
        // 点 (1,0) 先缩放 2 倍 → (2,0)，再旋转 90° → (0,2)，再平移 (10,0) → (10,2)
        Matrix3x2 matrix = Transform2DMath.Compose(new Vector2(10, 0), MathF.PI / 2, new Vector2(2, 2));

        Vector2 result = Vector2.Transform(new Vector2(1, 0), matrix);

        Assert.Equal(10f, result.X, 3);
        Assert.Equal(2f, result.Y, 3);
    }
}
