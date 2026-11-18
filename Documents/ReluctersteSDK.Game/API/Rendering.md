# API：精灵渲染

命名空间：`ReluctersteSDK.Game.Rendering`

- [`Sprite`](#sprite)：绘制参数组件
- [`RenderCommand`](#rendercommand)：一帧的绘制命令（快照结构）
- [`SpriteRenderSystem`](#spriterendersystem)：收集 + 排序 + 提交

---

## `Sprite`

```csharp
public sealed class Sprite
```

**是 class，不是 struct。** 原因：`Color` 的 `default` 是**全透明黑**，用 struct 时忘记设 `Tint` 会让精灵彻底不可见。引用类型可以在字段初始化器里给出 `Color.White`。

| 字段 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `Texture` | `Texture2D` | 构造参数 | 纹理 |
| `Source` | `Rectangle` | 整张纹理 | 采样区域 |
| `Tint` | `Color` | `Color.White` | 着色 |
| `Layer` | `int` | `0` | 层号，**升序先画** |
| `Depth` | `float` | `0f` | 同层深度，升序先画 |
| `Origin` | `Vector2` | `(0,0)` | 锚点（纹理空间） |
| `Effects` | `SpriteEffects` | `None` | 翻转等 |

| 构造 | 说明 |
|---|---|
| `Sprite(Texture2D texture)` | `Source` 为整张纹理；`texture` 为 null 抛 `ArgumentNullException` |
| `Sprite(Texture2D texture, Rectangle source)` | 指定采样区域 |

> `Layer` / `Depth` 都遵循"**小 = 先画 = 在下面**"。想画在 UI 最上层就给一个大 Layer。

---

## `RenderCommand`

```csharp
public struct RenderCommand
{
    public Texture2D? Texture;
    public Rectangle Source;
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
    public Vector2 Origin;
    public Color Tint;
    public SpriteEffects Effects;
    public int Layer;
    public float Depth;
    public int EntityIndex;
}
```

`SpriteRenderSystem.Execute` 每帧把所有可渲染实体解算成这个结构，`Submit` 再排序并提交。

`EntityIndex` 是**排序稳定性的最后一道保险**：同 Layer、同 Depth、同纹理的两个精灵按实体下标排序，帧间不会来回跳。

`Sprite` → `RenderCommand` 的转换：

| 字段 | 来源 |
|---|---|
| `Position` / `Rotation` / `Scale` | 世界矩阵（可再乘摄像机矩阵）经 `Transform2DMath.Decompose` 分解 |
| `Layer` / `Depth` / `Origin` / `Tint` / `Effects` / `Source` / `Texture` | 直接复制 |
| `EntityIndex` | `entity.Index` |

---

## `SpriteRenderSystem`

```csharp
public sealed class SpriteRenderSystem : ISystem
```

### 成员

| 成员 | 说明 |
|---|---|
| `Commands` | `IReadOnlyList<RenderCommand>`，上一帧收集到的**未排序**命令 |
| `Camera` | 可选 `Camera2D`；设置后输出屏幕坐标 |
| `CullingEnabled` | 是否按摄像机可见范围裁剪，默认 true |
| `LastCulledCount` | 上一帧被裁剪的精灵数量 |
| `Execute(World, float)` | 清空并重新收集 `(Transform, Sprite)` 实体 |
| `Submit(SpriteBatch)` | 排序后逐条 `Draw` |
| `static Compare(a, b)` | 公开的排序规则 |

### 执行流程

```csharp
// Game.Draw
world.Render(deltaTime);          // 触发 Execute，收集命令

spriteBatch.Begin(samplerState: SamplerState.PointClamp,
                  sortMode: SpriteSortMode.Deferred);
renderSystem.Submit(spriteBatch); // 排序 + 提交
spriteBatch.End();
```

### 矩阵来源

每个实体：

```csharp
Matrix3x2 m = world.TryGet<WorldTransform>(e, out var wt) ? wt.Value : transform.ToMatrix();
```

- 有 `WorldTransform` → 用世界矩阵（层级已累乘）；
- 没有 → 退回局部矩阵（扁平场景，不需要注册 `TransformSystem`）。

然后（可选）乘以 `Camera.ViewMatrix` 并做可见性裁剪，再 `Decompose` 出位置 / 旋转 / 缩放交给 `SpriteBatch.Draw`。

### 排序规则

```csharp
Layer 升序 → Depth 升序 → 纹理（RuntimeHelpers.GetHashCode）→ EntityIndex
```

- 前两层决定视觉遮挡；
- 第三层让**同纹理的相邻命令挨在一起**，便于 SpriteBatch 合批；
- 第四层保证完全相同的比较键下顺序稳定。

### 为什么 `Begin` 必须用 `Deferred`

`Submit` 在托管侧已经排好序。`SpriteBatch` 的其它排序模式（`Texture` / `BackToFront` / `FrontToBack`）会**在内部重排**，把 Layer 顺序打乱。

### 跳过条件

```csharp
if (sprite is null || sprite.Texture is null) return;
```

`Sprite` 引用为 null（理论上不该出现，除非把 null 塞进存储）或 `Texture` 为 null 时跳过。想临时隐藏精灵可以设 `Texture = null`，但更干净的做法是移除 `Sprite` 组件。

---

## 已知限制

- **纹理切换会打断合批**：排序只在同 Layer、同 Depth 内按纹理聚拢；跨层交错时合批会碎。
- **只有摄像机 AABB 裁剪**：设置 `Camera` 后按保守包围盒剔除；没有空间分区/四叉树。
- **`Commands` 在 `Submit` 后是已排序状态**：`Submit` 直接 `_commands.Sort`，所以属性描述里的"未排序"只对 `Execute` 到 `Submit` 之间成立。
- **不支持负缩放**：分解出的缩放恒为非负，翻转请用 `SpriteEffects`。
- **一个实体一个 `Sprite`**：同一实体画两层（阴影 + 本体）需要拆成两个实体。
- **`Origin` 不做自动居中**：默认左上角，需要居中请自己设 `new Vector2(w/2f, h/2f)`。
- **依赖 `SpriteSortMode.Deferred`**：换排序模式会静默失效，没有运行期检查。
- **两个 `Vector2` 会撞名**：`Sprite.Origin` / `RenderCommand.Position` 是 `Microsoft.Xna.Framework.Vector2`，而 `Transform2DMath` 用的是 `System.Numerics.Vector2`。同时 `using` 两个命名空间时 `Vector2` 会歧义，需要写全名——尤其是既算矩阵又画精灵的文件。
- **Math 向量到 MonoGame 向量**：使用 `MonoGameInterop.ToXna()` / `ToCartesian()` 显式转换。
