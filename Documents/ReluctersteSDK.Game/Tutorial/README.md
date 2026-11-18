# ReluctersteSDK.Game Tutorial

按主题组织的**完整用法示例**。每个文件都是一个可以独立阅读、按顺序抄进工程就能跑的小章节；示例之间共享同一套命名与项目结构。

> 想先知道这个包是什么、为什么这样设计，看 [../README.md](../README.md)；
> 想查某个方法到底返回什么，看 [../API/](../API/)。

---

## 阅读顺序

| # | 章节 | 你会得到 |
|---|---|---|
| 01 | [快速开始](01-快速开始.md) | 一个能跑的 MonoGame 最小工程：实体 + 系统 + 渲染 |
| 02 | [实体、组件与单例](02-实体组件与单例.md) | 创建/销毁实体、读写组件、单例组件、生命周期事件 |
| 03 | [查询与迭代安全](03-查询与迭代安全.md) | 1~4 组件查询、`Without`、`Count`、遍历中改结构的正确处理 |
| 04 | [层级与变换](04-层级与变换.md) | 父子关系、局部/世界矩阵、脏标记与 `TransformSystem` |
| 05 | [系统与调度](05-系统与调度.md) | 自定义系统、阶段、优先级、启停、`OnStart/OnStop` |
| 06 | [运动与输入](06-运动与输入.md) | 加速度、阻尼、限速、朝向，以及键盘驱动的玩家 |
| 07 | [渲染与摄像机](07-渲染与摄像机.md) | 精灵字段、层序、合批、摄像机跟随、裁剪与拾取 |
| 08 | [帧动画](08-帧动画.md) | 序列图切分、clip 管理、状态切换、一次性动画 |
| 09 | [双缓冲事件](09-双缓冲事件.md) | 帧内系统通信、伤害→死亡链路、桥接到事件总线 |
| 10 | [命令缓冲](10-命令缓冲.md) | 遍历中安全增删、生成/销毁、延迟层级与单例操作 |
| 11 | [快照与存档](11-快照与存档.md) | 注册组件、保存/读取 JSON、恢复世界、单例与层级 |
| 12 | [调试与统计](12-调试与统计.md) | 帧耗时、系统耗时、组件统计、控制台日志 |

第一次接触请按 01 → 02 → … 顺序读；已经有 ECS 经验的可以直接跳到需要的章节。

---

## 前置条件

- .NET SDK **10.0**；
- 一个 MonoGame DesktopGL 工程（01 给出了完整 `.csproj`）；
- 引用 `ReluctersteSDK.Game`：

```xml
<PackageReference Include="ReluctersteSDK.Game" Version="0.13.0.0" />
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.6.1" />
```

---

## 示例约定

- 示例默认放在命名空间 `MyGame` 下，你可以改成自己的；
- 纹理、字体等资源用**代码生成**（1×1 白纹理、`SpriteSheet.Slice`）或 `Content` 管线，示例会注明；
- `using` 只列当前章节真正用到的；省略的公共引用通常是：

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Game.Rendering;
using ReluctersteSDK.Game.Systems;
using ReluctersteSDK.Math.Vectors;
```

- 两个 `Vector2` 不要混用：
  - `CartesianVector2D` 属于 `ReluctersteSDK.Math.Vectors`，用于 `Transform` / `Velocity` 等 ECS 数据；
  - `Microsoft.Xna.Framework.Vector2` 用于 `Sprite.Origin` / `SpriteBatch.Draw`。
  - 互转用 `ReluctersteSDK.Game.Rendering.MonoGameInterop.ToXna()` / `ToCartesian()`。

---

## 一页速查

```csharp
var world = new World();

Entity e = world.Create();
world.Add(e, Transform.Identity);
world.Add(e, new Velocity { Value = new CartesianVector2D(60, 0) });
world.Add(e, new Sprite(texture));

world.AddSystem(new MotionSystem(), SystemPhase.Update, priority: -10);
world.AddSystem(new TransformSystem(), SystemPhase.Update, priority: 10);
var render = new SpriteRenderSystem();
world.AddSystem(render, SystemPhase.Render);

// 每帧
world.Update(dt);
world.Render(dt);
spriteBatch.Begin(sortMode: SpriteSortMode.Deferred);
render.Submit(spriteBatch);
spriteBatch.End();
```
