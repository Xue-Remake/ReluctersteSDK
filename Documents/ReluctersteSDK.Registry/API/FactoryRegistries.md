# API：类型 / 实例 / 委托注册表

命名空间：`ReluctersteSDK.Registry`

三者都实现空标记接口 `IRegistry`，并共享[统一启停语义](ValueRegistries.md#统一语义)。

- [`TypeRegistry`](#typeregistry)：抽象类型 → 具体类型 / 工厂，**每次取用都新建**
- [`InstanceRegistry`](#instanceregistry)：类型 → 单例 / 懒加载单例
- [`DelegateRegistry`](#delegateregistry)：字符串 → 委托

---

## `TypeRegistry`

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Register<TAbstract, TConcrete>()` | `void` | 约束 `TConcrete : class, TAbstract`，内部用 `Activator.CreateInstance` |
| `Register<TAbstract>(Func<TAbstract> factory)` | `void` | 工厂方式 |
| `CreateInstance<TAbstract>()` | `TAbstract` | 未注册 / 已禁用 / 工厂返回 null → **抛 `InvalidOperationException`** |
| `TryCreateInstance<TAbstract>(out TAbstract instance)` | `bool` | 同上情况返回 `false`（`instance` 为 `default`） |
| `Enable<TAbstract>()` / `Disable<TAbstract>()` | `bool` | |
| `Remove<TAbstract>()` | `bool` | 删除映射 |
| `Exists<TAbstract>()` / `IsEnabled<TAbstract>()` | `bool` | |
| `EnabledAbstractTypes()` | `ICollection<Type>` | 已启用的抽象类型 |
| `Clear()` | `void` | |

### 两种来源互斥

`Register` 会**整条替换**映射：注册具体类型会把工厂清空，注册工厂会把具体类型清空。这样不会出现"工厂优先导致新映射被静默忽略"。

```csharp
var types = new TypeRegistry();

types.Register<IRepository, SqlRepository>();
IRepository a = types.CreateInstance<IRepository>();   // 每次都是新实例

types.Register<IRepository>(() => new SqlRepository());
IRepository b = types.CreateInstance<IRepository>();   // 走工厂
```

### 具体类型构造的要求

反射路径要求 `TConcrete` 有**公共无参构造函数**。没有时 `CreateInstance` 抛 `InvalidOperationException`（内部异常保留原始反射异常）。需要参数就改用工厂重载。

### 禁用后的行为

`Disable<TAbstract>()` 后 `CreateInstance` 抛 `InvalidOperationException`，`TryCreateInstance` 返回 `false`。重新 `Enable` 恢复原映射。

---

## `InstanceRegistry`

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `RegisterInstance<T>(T instance)` | `void` | 注册现成实例，`where T : notnull` |
| `RegisterFactory<T>(Func<T> factory)` | `void` | 懒加载：首次 `Resolve<T>()` 时创建并缓存 |
| `Resolve<T>()` | `T` | 未注册 / 已禁用 → **抛 `InvalidOperationException`** |
| `TryResolve<T>(out T instance)` | `bool` | |
| `Enable<T>()` / `Disable<T>()` | `bool` | |
| `Remove<T>()` | `bool` | |
| `Exists<T>()` / `IsEnabled<T>()` | `bool` | |
| `EnabledTypes()` | `ICollection<Type>` | |
| `Clear()` | `void` | |

### 键是注册时的泛型参数

```csharp
services.RegisterInstance(new DataService());

services.Resolve<DataService>();       // ✅
services.Resolve<IDataService>();      // ❌ 未注册 —— 键是 DataService，不是接口
```

想按接口解析：

```csharp
services.RegisterInstance<IDataService>(new DataService());
services.Resolve<IDataService>();      // ✅
```

### 工厂的并发与异常

工厂包在 `Lazy<object>(factory, LazyThreadSafetyMode.PublicationOnly)` 里：

- 并发首次 `Resolve` 时，多个工厂可能都执行，但**只有第一个成功的结果被采用**，后续调用返回同一个；
- **工厂抛出的异常不会被缓存**，下次 `Resolve` 会重新执行工厂；
- 注册 `RegisterInstance(null)` 在 `T` 为非空引用类型时由 `notnull` 约束挡住；值类型 `T` 无此问题（但 `default` 会被当成合法实例）。

### 与 `TypeRegistry` 的区别

| | `TypeRegistry` | `InstanceRegistry` |
|---|---|---|
| 回答什么问题 | "给我造一个新的" | "给我那个共享的" |
| 每次调用 | 新实例 | 同一实例 |
| 典型用途 | 无状态服务、仓储、策略 | 缓存、配置、连接、状态持有者 |

两者**不会互相查找**：在 `TypeRegistry` 里注册的工厂，`InstanceRegistry` 是看不见的。

---

## `DelegateRegistry`

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Register(string name, Delegate del)` | `void` | 基础重载 |
| `Register(string name, Action action)` | `void` | |
| `Register<T>(string name, Action<T> action)` | `void` | |
| `Register<TResult>(string name, Func<TResult> func)` | `void` | |
| `Register<T,TResult>(string name, Func<T,TResult> func)` | `void` | |
| `Invoke(string name)` | `void` | 无参 `Action`；签名不符抛 `InvalidOperationException` |
| `Invoke<TResult>(string name)` | `TResult` | 无参 `Func<TResult>` |
| `Invoke<T>(string name, T arg)` | `void` | 单参 `Action<T>` |
| `Invoke<T,TResult>(string name, T arg)` | `TResult` | 单参 `Func<T,TResult>` |
| `TryGetDelegate(string name, out Delegate del)` | `bool` | 拿原始委托，可自行 `DynamicInvoke` |
| `Enable(string)` / `Disable(string)` | `bool` | |
| `Remove(string)` | `bool` | |
| `Exists(string)` / `IsEnabled(string)` | `bool` | |
| `EnabledNames()` | `ICollection<string>` | |
| `Clear()` | `void` | |

### 调用路径

`Invoke` 先尝试把委托强转成期望的 `Action` / `Func<...>` 直接调用；转换失败才回退到 `Delegate.DynamicInvoke`。

- 直接路径：快，异常原样抛出；
- 回退路径：慢，异常会被包进 `InvalidOperationException`（`InnerException` 是原始异常）。

只有 `Register(string, Delegate)` 注册任意签名的委托时才会走回退路径。用强类型重载注册的单参委托仍然走直接路径。

### 名字匹配

- 区分大小写（`StringComparer.Ordinal`）；
- `Register` 同名覆盖；
- 被禁用的名字 `Invoke` 抛 `InvalidOperationException`，`TryGetDelegate` 返回 `false`。

### 无参 / 单参限制

只提供 0 参和 1 参的 `Invoke` 便利重载。多参签名请用 `TryGetDelegate` 拿原始委托后自行 `DynamicInvoke`：

```csharp
if (actions.TryGetDelegate("move", out Delegate del))
    del.DynamicInvoke(10, 20);
```

---

## 共同限制

- **没有生命周期管理**：`InstanceRegistry` 不会释放它持有的实例（不调用 `Dispose`），需要自己管。
- **没有作用域**：不存在 Scoped/Transient 概念；`TypeRegistry` 全是 Transient，`InstanceRegistry` 全是 Singleton。
- **没有构造函数注入**：工厂闭包要自己捕获依赖。
- **没有循环依赖检测**：A 的工厂里 `Resolve<B>()`、B 的工厂里 `Resolve<A>()` 会栈溢出。
- **`DelegateRegistry` 无参数类型校验**：`Invoke` 的泛型参数只在运行期与委托签名比对，写错要到调用时才报错。
