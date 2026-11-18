# API：帧动画

命名空间：`ReluctersteSDK.Game.Rendering`

| 类型 | 角色 |
|---|---|
| `AnimationFrame` | 一帧：源矩形 + 停留时长 |
| `AnimationClip` | 命名动画：帧序列 + 是否循环（**不可变，可共享**） |
| `Animator` | 组件：持有多个 clip 与播放状态 |
| `SpriteSheet` | 序列图切分工具 |
| `SpriteAnimationSystem` | 每帧推进 `Animator` 并把当前帧写进 `Sprite.Source` |

---

## `AnimationFrame`

```csharp
public readonly struct AnimationFrame
{
    public readonly Rectangle Source;
    public readonly float Duration;

    public AnimationFrame(Rectangle source, float duration);
}
```

`Duration` 单位为**秒**，构造时校验：`<= 0`、`NaN`、`Infinity` 一律抛 `ArgumentOutOfRangeException`。

> 这条校验不是洁癖。`Animator.Tick` 用 `while (remaining > 0)` 推进，某帧时长为 0 会让 `left = 0`、`remaining` 永不减少 → **死循环**。在构造期拦住比在热路径上每次判断便宜得多。

---

## `AnimationClip`

```csharp
public sealed class AnimationClip
{
    public string Name { get; }
    public bool Loop { get; }
    public float TotalDuration { get; }
    public int FrameCount { get; }
    public IReadOnlyList<AnimationFrame> Frames { get; }
    public AnimationFrame this[int index] { get; }

    public AnimationClip(string name, IEnumerable<AnimationFrame> frames, bool loop = true);
    public AnimationClip(string name, IEnumerable<Rectangle> sources, float frameDuration, bool loop = true);
}
```

### 构造规则

| 情况 | 行为 |
|---|---|
| `name` 为 null / 空白 | 抛 `ArgumentException` |
| `frames` 为空 | 抛 `ArgumentException` |
| `frameDuration <= 0`（统一时长重载） | 抛 `ArgumentOutOfRangeException` |
| `frames` 为 null | 抛 `ArgumentNullException` |

### 不可变

`Frames` 是 `ReadOnlyCollection<AnimationFrame>` 视图，无法强转回数组后篡改。`TotalDuration` 在构造时算好。

因此 **clip 应该全局共享**，不要每个实体 `new` 一份：

```csharp
static readonly AnimationClip Run = new AnimationClip("run", runFrames, 0.1f);
```

### 两种构造

```csharp
// 统一帧时长
var idle = new AnimationClip("idle", idleFrames, frameDuration: 0.25f, loop: true);

// 每帧独立时长（关键帧多停几拍）
var attack = new AnimationClip("attack", new[]
{
    new AnimationFrame(new Rectangle(0,  0, 32, 32), 0.06f),
    new AnimationFrame(new Rectangle(32, 0, 32, 32), 0.20f),
    new AnimationFrame(new Rectangle(64, 0, 32, 32), 0.10f),
}, loop: false);
```

---

## `Animator`

```csharp
public sealed class Animator
```

**引用类型**，作为组件挂在实体上。

### 构造与 clip 管理

| 成员 | 说明 |
|---|---|
| `Animator(params AnimationClip[] clips)` | 同名 clip **后者覆盖前者**；null 元素被忽略 |
| `AddClip(AnimationClip)` | 注册或覆盖；`clip` 为 null 抛 `ArgumentNullException` |
| `HasClip(string name)` | 名称区分大小写（`StringComparer.Ordinal`） |
| `ClipCount` / `ClipNames` | |

### 状态

| 属性 | 类型 | 说明 |
|---|---|---|
| `CurrentClip` | `AnimationClip?` | 未播放过时为 `null` |
| `CurrentFrameIndex` | `int` | 当前帧下标 |
| `CurrentFrameTime` | `float` | 当前帧已停留秒数 |
| `IsPlaying` | `bool` | |
| `Speed` | `float` | 速度倍率；非正数、NaN、Infinity 会被忽略并保持上一次有效值 |
| `CurrentSource` | `Rectangle?` | 当前帧矩形；无 clip 或下标越界时为 `null` |
| `FinishedClip` | `string?` | 本帧刚播完的**非循环** clip 名 |
| `FrameChanged` | `bool` | 本帧内帧下标是否变化过 |

### 控制方法

| 方法 | 说明 |
|---|---|
| `Play(string name, bool restart = false)` | 名称不存在返回 `false`，当前动画不受影响 |
| `Stop()` | 停止并回到第一帧（当前帧仍可被渲染） |
| `Pause()` | 暂停在当前帧（`IsPlaying = false`，进度保留） |
| `Resume()` | 从暂停处继续；从未播放过时无效果 |
| `Tick(float deltaTime)` | 推进时间，通常由 `SpriteAnimationSystem` 调用 |

### `Play` 的两种模式

```csharp
animator.Play("run");                 // 若正播放 "run"，保持进度不变
animator.Play("run", restart: true);  // 强制从第 0 帧重来
```

判定用的是**引用相等**（`ReferenceEquals(CurrentClip, clip)`），不是名字。传入的 clip 对象不同就会被当成新动画。

`Play` 成功时会：切换到目标 clip、`_frameIndex = 0`、`_frameTime = 0`、`IsPlaying = true`、清空 `FinishedClip`、**置 `FrameChanged = true`**。

### `Tick` 的推进算法

```csharp
FinishedClip = null;
FrameChanged = false;
if (!IsPlaying || CurrentClip is null || deltaTime <= 0f || Speed <= 0f) return;

float remaining = deltaTime * Speed;
while (remaining > 0f)
{
    float left = frame.Duration - _frameTime;
    if (remaining < left) { _frameTime += remaining; break; }   // 没跨帧
    remaining -= left; _frameTime = 0f;

    if (还有下一帧)      _frameIndex++;  FrameChanged = true;
    else if (clip.Loop)  _frameIndex = 0; FrameChanged = true;
    else               { IsPlaying = false; FinishedClip = clip.Name; break; }
}
```

要点：

- **单次调用可跨任意多帧**：`Tick(0.35f)` 在 0.1s 帧长下直接推进到第 3 帧的合理位置，不会"欠帧"；
- 掉帧、暂停恢复后不会积压时间；
- 非循环动画播完后**停在最后一帧**，`IsPlaying = false`，`CurrentSource` 仍是末帧矩形。

### `FrameChanged` / `FinishedClip` 的生命周期

这两个字段在**每次 `Tick` 开头清空**，只在本次推进中置位。

因此消费它们的系统必须排在 `SpriteAnimationSystem` **之后**：

```csharp
world.AddSystem(new SpriteAnimationSystem());
world.AddSystem(new PlaybackStateSystem());   // 在这里读 FinishedClip 才有效
```

同理，在 `Tick` 之前读到的永远是上一帧的结果（或 `null`）。

---

## `SpriteSheet`

```csharp
public static class SpriteSheet
{
    public static Rectangle[] Slice(int textureWidth, int textureHeight,
                                    int frameWidth, int frameHeight,
                                    int columns = 0, int startIndex = 0, int? frameCount = null);

    public static Rectangle[] Slice(Texture2D texture, int frameWidth, int frameHeight,
                                    int columns = 0, int startIndex = 0, int? frameCount = null);
}
```

| 参数 | 说明 |
|---|---|
| `columns` | 每行帧数；`<= 0` 时按 `textureWidth / frameWidth` 自动推算 |
| `startIndex` | 起始帧下标，**行优先**编号 |
| `frameCount` | 取出的帧数；`null` 表示取到末尾 |

编号方式：

```
0 1 2
3 4 5     ← 48×32 切 16×16，columns=3，rows=2
```

### 校验

以下情况抛 `ArgumentOutOfRangeException`：纹理宽高 ≤ 0、帧宽高 ≤ 0、`startIndex < 0`、推算出的列数 ≤ 0、`frameCount < 0`、请求范围超出总帧数、某个矩形越出纹理边界。

最后一条是给"纹理尺寸不是帧尺寸整数倍"准备的——例如 48×33 切 16×16 时，第二行的格子会越界。

---

## `SpriteAnimationSystem`

```csharp
public sealed class SpriteAnimationSystem : ISystem
```

对每个 `(Sprite, Animator)` 实体：

```csharp
animator.Tick(deltaTime);
Rectangle? source = animator.CurrentSource;
if (source.HasValue) sprite.Source = source.Value;
```

`CurrentSource` 为 `null`（没播过任何动画）时**不改 `Sprite.Source`**，保持原值。

**注册位置**：必须在 `SpriteRenderSystem` 之前。

---

## 已知限制

- **帧只支持矩形采样区**：不支持旋转、描点、网格形变。
- **一个实体一个 `Animator`**：同一时间只播一段动画。需要"上下半身各播一套"就拆成父子实体。
- **没有帧事件**：不能"第 3 帧生成一次攻击判定"。可以用 `FrameChanged` + `CurrentFrameIndex` 轮询，或在消费系统里自己记上一帧的下标。
- **没有过渡 / 混合**：`Play` 是硬切，没有淡入淡出。
- **`Speed` 不支持倒放**：负值会被忽略；顺序播放用正数，需要倒放请自行扩展。
- **超大 `deltaTime` 有循环保护**：`Tick` 单次最多推进 10000 帧，避免无限循环。
- **`Play` 置 `FrameChanged = true`**：切动画那一帧即使没真正换帧，消费方也会收到"变了"。
- **clip 名称区分大小写**：`"Run"` 和 `"run"` 是两个不同的动画。
- **`AnimationClip` 不可变，但 `Animator` 可变**：多个实体可以共享 clip，但每个实体要有自己的 `Animator`。
