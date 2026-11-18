using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Test.Geometry;

public class GeometryTests
{
    [Fact]
    public void Aabb_Normalizes_And_Contains()
    {
        var box = new Aabb2D(new CartesianVector2D(2, 3), new CartesianVector2D(0, 0));

        Assert.Equal(new CartesianVector2D(0, 0), box.Min);
        Assert.Equal(new CartesianVector2D(2, 3), box.Max);
        Assert.True(box.Contains(new CartesianVector2D(1, 1)));
        Assert.False(box.Contains(new CartesianVector2D(3, 1)));
    }

    [Fact]
    public void Aabb_Intersects_And_ClosestPoint()
    {
        var a = new Aabb2D(0, 0, 4, 4);
        var b = new Aabb2D(3, 3, 4, 4);

        Assert.True(a.Intersects(b));
        Assert.Equal(new CartesianVector2D(4, 4), a.ClosestPoint(new CartesianVector2D(10, 10)));
        Assert.Equal(new CartesianVector2D(3, 3), a.Intersection(b).Min);
        Assert.Equal(new CartesianVector2D(4, 4), a.Intersection(b).Max);
    }

    [Fact]
    public void Circle_Contains_And_Intersects()
    {
        var circle = new Circle2D(new CartesianVector2D(0, 0), 2f);

        Assert.True(circle.Contains(new CartesianVector2D(1, 1)));
        Assert.False(circle.Contains(new CartesianVector2D(2, 2)));
        Assert.True(circle.Intersects(new Circle2D(new CartesianVector2D(3, 0), 1.5f)));
        Assert.True(circle.Intersects(new Aabb2D(1.5f, -1, 2, 2)));
    }

    [Fact]
    public void Segment_ClosestPoint_And_Intersection()
    {
        var segment = new Segment2D(new CartesianVector2D(0, 0), new CartesianVector2D(10, 0));

        Assert.Equal(new CartesianVector2D(3, 0), segment.ClosestPoint(new CartesianVector2D(3, 5)));
        Assert.Equal(5f, segment.DistanceTo(new CartesianVector2D(3, 5)), 3);

        var other = new Segment2D(new CartesianVector2D(5, -5), new CartesianVector2D(5, 5));
        Assert.True(segment.Intersects(other, out CartesianVector2D point));
        Assert.True(point.ApproximatelyEquals(new CartesianVector2D(5, 0), 1e-5f));
    }

    [Fact]
    public void Parallel_Segments_Do_Not_Intersect()
    {
        var a = new Segment2D(new CartesianVector2D(0, 0), new CartesianVector2D(10, 0));
        var b = new Segment2D(new CartesianVector2D(0, 1), new CartesianVector2D(10, 1));

        Assert.False(a.Intersects(b, out _));
    }

    [Fact]
    public void Ray_Intersects_Circle_And_Aabb()
    {
        var ray = new Ray2D(new CartesianVector2D(-10, 0), new CartesianVector2D(1, 0));

        Assert.True(ray.Intersects(new Circle2D(new CartesianVector2D(0, 0), 2f), out float circleDistance));
        Assert.Equal(8f, circleDistance, 3);

        Assert.True(ray.Intersects(new Aabb2D(-1, -1, 2, 2), out float boxDistance));
        Assert.Equal(9f, boxDistance, 3);
    }

    [Fact]
    public void Triangle_Contains_And_Barycentric()
    {
        var triangle = new Triangle2D(
            new CartesianVector2D(0, 0),
            new CartesianVector2D(10, 0),
            new CartesianVector2D(0, 10));

        Assert.True(triangle.Contains(new CartesianVector2D(1, 1)));
        Assert.False(triangle.Contains(new CartesianVector2D(8, 8)));

        triangle.Barycentric(new CartesianVector2D(0, 0), out float u, out float v, out float w);
        Assert.Equal(1f, u, 3);
        Assert.Equal(0f, v, 3);
        Assert.Equal(0f, w, 3);
    }

    [Fact]
    public void Polygon_Area_Contains_And_Convexity()
    {
        var square = new Polygon2D(new[]
        {
            new CartesianVector2D(0, 0),
            new CartesianVector2D(10, 0),
            new CartesianVector2D(10, 10),
            new CartesianVector2D(0, 10)
        });

        Assert.Equal(100f, square.Area, 3);
        Assert.True(square.Contains(new CartesianVector2D(5, 5)));
        Assert.False(square.Contains(new CartesianVector2D(15, 5)));
        Assert.True(square.IsConvex);
        Assert.Equal(4, square.Edges().Count());
    }

    [Fact]
    public void Concave_Polygon_Is_Detected()
    {
        var arrow = new Polygon2D(new[]
        {
            new CartesianVector2D(0, 0),
            new CartesianVector2D(4, 4),
            new CartesianVector2D(0, 8),
            new CartesianVector2D(1, 4)
        });

        Assert.False(arrow.IsConvex);
    }
}
