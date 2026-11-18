# API：接口定义

命名空间：`ReluctersteSDK.Config.Interfaces`

这些接口**全是契约**，没有任何实现。具体存储结构（格子制 / 载重制 / 字典）由使用方决定。

---

## `IConfig`

```csharp
public interface IConfig { }
```

**纯标记接口**。实现它就等于声明"这个类是配置数据模型，可以被任意 `IConfigStore<T>` 读写"。

由于 `IConfigStore<T>` 的泛型约束是 `class, IConfig, new()`，实现方还必须有**公共无参构造函数**；走 SQLite 后端时再配合 ORM 的特性：

```csharp
[DbTable("settings")]
public class GameSetting : IConfig
{
    [DbKey("key")]    public string Key { get; set; } = "";
    [DbColumn("val")] public string Value { get; set; } = "";
}
```

---

## `IData<TEntity>` 与 `IEntity<TSelf, TData>`

```csharp
public interface IData<TEntity>
{
    TEntity ToEntity();
}

public interface IEntity<TSelf, TData>
    where TSelf : IEntity<TSelf, TData>
    where TData : IData<TSelf>
{
    TData ToData();
}
```

CRTP（奇异递归模板）写法：`TSelf` 是**自己**的类型，这样 `ToData()` 返回的 `TData` 的 `ToEntity()` 才能转回 `TSelf`。

### 两层的分工

| 层 | 接口 | 职责 |
|---|---|---|
| 业务模型 | `IEntity` | 内存里用的对象，带行为和计算属性 |
| 数据行 | `IData` | 落库用的扁平结构，字段与表一一对应 |

```csharp
// 数据行
[DbTable("items")]
public class ItemDataRow : IItemData<ItemEntity>
{
    [DbKey("item_id")]   public int ItemId { get; set; }
    [DbColumn("name")]   public string Name { get; set; } = "";
    [DbColumn("rarity")] public int Rarity { get; set; }

    public ItemEntity ToEntity() => new ItemEntity { /* 映射 */ };
}

// 业务模型
public class ItemEntity : IItemEntity<ItemEntity, ItemDataRow>
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public int Rarity { get; set; }

    public ItemDataRow ToData() => new ItemDataRow { ItemId = ItemId, Name = Name, Rarity = Rarity };
}
```

调用 `EntityConfigStore<TEntity, TData>` 时，泛型参数 `TEntity` **必须实现对应的接口**（例如 `IItemEntity<TEntity, TData>`），`TData` 则必须实现 `IItemData<TEntity>` 且有公共无参构造函数。

实际上真正入库的**只有 `TData`**：`SaveAsync` 内部是 `store.SaveAsync(entity.ToData())`。所以 ORM 特性（`[DbTable]` / `[DbKey]`）标在 `TData` 上，**不在 `TEntity` 上**。

---

## `IItemData` / `IItemEntity`

```csharp
public interface IItemData<TEntity> : IData<TEntity>, IConfig { }   // 无额外成员

public interface IItemEntity<TSelf, TData> : IEntity<TSelf, TData>
    where TSelf : IItemEntity<TSelf, TData>
    where TData : IItemData<TSelf>
{
    string Name { get; }
    int ItemId { get; }
}
```

`IItemData` 只是把 `IData` 特化一下并叠加 `IConfig`，本身不加成员 —— 这样数据行可以直接放进任意 `IConfigStore<TData>`。

---

## `IInventoryData` / `IInventoryEntity`

```csharp
public interface IInventoryData<TEntity> : IData<TEntity>, IConfig
{
    int InventoryId { get; }
}

public interface IInventoryEntity<TSelf, TData> : IEntity<TSelf, TData>
    where TSelf : IInventoryEntity<TSelf, TData>
    where TData : IInventoryData<TSelf>
{
    int InventoryId { get; }

    int  GetItemCount(int itemId);
    bool Contains(int itemId, int count = 1);
    bool CanPush (int itemId, int count = 1);
    bool CanPop  (int itemId, int count = 1);

    bool Push(int itemId, int count);
    bool Pop (int itemId, int count);
    bool TryPush(int itemId, int count, out int unaddedCount);
    bool TryPop (int itemId, int count, out int poppedCount);

    void Clear();
}
```

### 方法语义

| 方法 | 说明 |
|---|---|
| `GetItemCount` | 指定物品的数量；不存在返回 0 |
| `Contains` | 数量是否 ≥ `count` |
| `CanPush` / `CanPop` | **预判**，不改变状态 |
| `Push` / `Pop` | 全有或全无；放不下 / 取不够返回 `false` |
| `TryPush` / `TryPop` | 尽量全部，`out` 报告未处理数量；**返回 `bool` 表示是否处理了至少一部分** |
| `Clear` | 清空 |

> 这些是**契约**，库不会调用它们——`EntityConfigStore` 只做转换与持久化，业务逻辑全靠你的实现。

---

## 已知限制

- **接口零实现**：库存的格子/载重逻辑、物品的属性计算全都要自己写。
- **`IEntity` 的 `TSelf` 不能用接口做泛型参数**：必须传具体类，否则 `TData.ToEntity()` 的返回类型对不上。
- **`IItemData` / `IInventoryData` 现在同时是 `IConfig`**：因此数据行可以直接交给任意 `IConfigStore<TData>`；这一点在旧版本中由各 Manager 隐式承担。
