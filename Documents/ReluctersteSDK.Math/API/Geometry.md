# API：2D 几何

命名空间：`ReluctersteSDK.Math.Geometry`

所有几何体为 `readonly struct`（`Polygon2D` 除外），退化输入返回 `false`/零值，不抛异常。

## `Aabb2D`

轴对齐包围盒。

| 成员 | 说明 |
|---|---|
| `Min` / `Max` | 最小/最大角点（构造时自动归一化） |
| `Center` / `Size` / `Extents` / `Area` / `IsEmpty` | 派生量 |
| `FromCenterSize(center, size)` / `FromPoints(...)` | 构造 |
| `Contains(point)` / `Contains(Aabb2D)` | 包含测试（边界算包含） |
| `Intersects(Aabb2D)` / `Intersects(Circle2D)` | 相交测试 |
| `Intersection(other)` / `Union(other)` | 交集 / 并集 |
| `Inflate(amount)` / `Expand(amount)` | 扩张/收缩 |
| `ClosestPoint(point)` | 盒内最近点 |
| `Transform(Matrix3x2)` | 变换四角后的新 AABB |

## `Circle2D`

| 成员 | 说明 |
|---|---|
| `Center` / `Radius` / `Area` | 半径为负或非有限数时抛 `ArgumentOutOfRangeException` |
| `Contains(point)` / `Contains(Circle2D)` | |
| `Intersects(Circle2D)` / `Intersects(Aabb2D)` | 相切算相交 |
| `ClosestPoint(point)` / `DistanceTo(point)` | |
| `Inflate(amount)` | 半径不小于 0 |

## `Segment2D`

| 成员 | 说明 |
|---|---|
| `A` / `B` / `Length` / `SqrLength` / `Direction` | |
| `GetPoint(t)` / `ClosestPoint(point)` / `DistanceTo(point)` | |
| `Intersects(Segment2D, out point)` | 共线重叠时返回一个重叠端点 |
| `Intersects(Circle2D)` / `Intersects(Aabb2D)` | |

## `Ray2D`

| 成员 | 说明 |
|---|---|
| `Origin` / `Direction` | 方向自动归一化；零向量抛 `ArgumentException` |
| `GetPoint(distance)` | |
| `Intersects(Circle2D/Aabb2D/Segment2D, out float distance)` | 输出最近的非负距离 |

## `Triangle2D`

`A` / `B` / `C` / `Area` / `Centroid` / `Bounds` / `Contains(point)` / `Barycentric(point, out u, out v, out w)` / `ClosestPoint(point)`。

## `Polygon2D`

简单多边形（支持凹多边形，自交结果未定义）。

| 成员 | 说明 |
|---|---|
| `Points` / `Count` | 只读顶点列表 |
| `Area` / `SignedArea` / `Centroid` / `Bounds` | |
| `Contains(point)` | 奇偶规则；边界结果未定义 |
| `IsConvex` | 凸性判断 |
| `Edges()` | 枚举边 |

## `Geometry2D`

静态助手：`ClosestPointOnSegment`、`PointInTriangle`、`PointInPolygon`、`SegmentIntersectsSegment`、`SegmentIntersectsCircle`、`SegmentIntersectsAabb`、`RayIntersectsAabb`、`Orientation`、`AabbFromPoints`。

## 已知限制

- 只提供 float 精度；双精度几何体首版未实现。
- `Polygon2D.Contains` 对自交多边形结果未定义。
- 相交测试使用 `1e-6f` 容差。
