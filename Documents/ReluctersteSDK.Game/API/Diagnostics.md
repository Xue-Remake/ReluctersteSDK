# API：调试统计

命名空间：`ReluctersteSDK.Game.Ecs`

## 开启

```csharp
world.StatisticsEnabled = true;
```

默认关闭，避免常态开销。

## `WorldStats`

`GetStats()` 返回：

| 成员 | 说明 |
|---|---|
| `EntityCount` / `EntityCapacity` | 实体数量与槽位容量 |
| `ComponentStoreCount` / `ComponentCount` | 组件存储类型数与组件实例总数 |
| `UpdateSystemCount` / `LateUpdateSystemCount` / `RenderSystemCount` | 各阶段系统数 |
| `StructuralVersion` | 当前结构版本 |
| `IsDisposed` | 是否已释放 |
| `FrameCount` | Update 帧数 |
| `UpdateMilliseconds` / `RenderMilliseconds` | 上一帧总耗时 |

## `SystemTiming`

`world.SystemTimings` 返回上一帧各系统的 `SystemName`、`Phase`、`Milliseconds`。

## `ComponentStoreStats`

`GetComponentStats()` 返回每个组件类型的 `ComponentType` 与 `Count`。

## 已知限制

- 时间采样基于 `Stopwatch`，只用于诊断，不保证跨平台精度。
- 开启统计会产生少量分配；生产环境建议按需开启。
