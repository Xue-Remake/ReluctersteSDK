# API：快照

命名空间：`ReluctersteSDK.Game.Ecs.Serialization`

## 注册表

```csharp
var registry = ComponentCodecRegistry.CreateDefault();
registry.Register<Health>();
registry.RegisterJson<Inventory>();   // Register<T> 的别名
```

`CreateDefault()` 注册 `Transform`、`Parent`、`Velocity`、`Acceleration`、`Damping`、`Heading`。
只有注册过的类型才会参与快照。

## 捕获与恢复

```csharp
WorldSnapshot snapshot = WorldSnapshotSerializer.Capture(world, registry);
WorldSnapshotSerializer.Restore(targetWorld, snapshot, registry);

// 或直接走流
WorldSnapshotSerializer.Save(world, stream, registry);
WorldSnapshotSerializer.Load(targetWorld, stream, registry);
```

## 选项

| 选项 | 默认 | 说明 |
|---|---|---|
| `ClearBeforeRestore` | `true` | 目标世界非空时先清空实体/组件（保留系统） |
| `IncludeSingletons` | `true` | 是否保存单例组件 |
| `ThrowOnUnsupportedComponent` | `true` | 遇到未注册类型是否抛异常 |
| `JsonOptions` | `SnapshotJson.CreateOptions()` | JSON 选项与转换器 |

## 语义

- 恢复期间会按正常路径触发 `EntityCreated` / `ComponentAdded` / `ComponentRemoved`，事件处理器不要假设组件已全部就绪。
- 保存每个存活实体的 `Index` + `Generation`，恢复后句柄保持一致。
- `Parent` 会被保存；`Children` 由 `Parent` 重建。
- `WorldTransform` **不保存**，恢复后由 `TransformSystem` 重算。
- 不支持 `Sprite`、`Animator`、`Texture2D`、系统实例、事件订阅、命令缓冲。
- 恢复时只允许注册表内的类型，避免按 JSON 字符串反射加载任意类型。

## 已知限制

- 不是完整存档：运行时资源与渲染状态不在快照内。
- 自定义组件需要能被 `System.Text.Json` 序列化；readonly 字段类型需提供转换器。
