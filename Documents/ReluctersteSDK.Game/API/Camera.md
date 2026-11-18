# API：Camera2D

命名空间：`ReluctersteSDK.Game.Rendering`

## 属性

| 成员 | 说明 |
|---|---|
| `Position` | 摄像机中心的世界坐标 |
| `Rotation` | 旋转弧度（正值逆时针） |
| `Zoom` | 缩放倍率，必须 > 0；非法值被忽略 |
| `ViewportWidth` / `ViewportHeight` | 视口像素尺寸，必须 > 0 |
| `ViewMatrix` | 世界 → 屏幕矩阵 |
| `ViewBounds` | 摄像机在 world 空间的可见 `Aabb2D` |

## 方法

| 方法 | 说明 |
|---|---|
| `Resize(width, height)` | 调整视口 |
| `WorldToScreen(CartesianVector2D)` | 世界 → 屏幕 |
| `ScreenToWorld(CartesianVector2D)` | 屏幕 → 世界 |

## 与 SpriteRenderSystem 配合

```csharp
renderSystem.Camera = camera;
renderSystem.CullingEnabled = true;
```

- `Execute` 把世界矩阵乘以 `ViewMatrix`，输出屏幕坐标的 `RenderCommand`。
- 裁剪使用 sprite 四角变换后的保守 AABB 与 `ViewBounds` 相交。
- `LastCulledCount` 记录上一帧被裁剪的数量。
- `Submit` 的 `SpriteBatch.Begin` 仍由调用方负责，建议 `SpriteSortMode.Deferred`。

## 已知限制

- 只支持 2D 正交视图；没有透视投影。
- 裁剪是保守 AABB，不做 OBB 精确裁剪。
- 视口尺寸需要调用方从 `GraphicsDevice.Viewport` 同步。
