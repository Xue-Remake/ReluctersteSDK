using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 委托注册表：按名称存储委托，支持对单个委托条目启用/禁用（线程安全）。
    /// 条目不可变，更新通过 ConcurrentDictionary 原子替换完成。
    /// </summary>
    public class DelegateRegistry
    {
        private sealed class DelegateEntry
        {
            public readonly Delegate Delegate;
            public readonly bool Enabled;

            public DelegateEntry(Delegate del, bool enabled)
            {
                Delegate = del;
                Enabled = enabled;
            }

            public DelegateEntry WithEnabled(bool enabled) => new(Delegate, enabled);
        }

        private readonly ConcurrentDictionary<string, DelegateEntry> _delegates;

        public DelegateRegistry()
        {
            _delegates = new ConcurrentDictionary<string, DelegateEntry>();
        }

        /// <summary>
        /// 注册委托。新项默认启用；若名称已存在则替换委托并保持原启用状态。
        /// </summary>
        public void Register(string name, Delegate del)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (del == null) throw new ArgumentNullException(nameof(del));

            _delegates.AddOrUpdate(
                name,
                _ => new DelegateEntry(del, true),
                (_, existing) => new DelegateEntry(del, existing.Enabled));
        }

        // 便捷重载
        public void Register(string name, Action action) => Register(name, (Delegate)action);
        public void Register<T>(string name, Action<T> action) => Register(name, (Delegate)action);
        public void Register<TResult>(string name, Func<TResult> func) => Register(name, (Delegate)func);
        public void Register<T, TResult>(string name, Func<T, TResult> func) => Register(name, (Delegate)func);

        /// <summary>启用指定名称的委托</summary>
        public bool Enable(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return SetEnabled(name, true);
        }

        /// <summary>禁用指定名称的委托</summary>
        public bool Disable(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return SetEnabled(name, false);
        }

        private bool SetEnabled(string name, bool enabled)
        {
            while (_delegates.TryGetValue(name, out var entry))
            {
                if (entry.Enabled == enabled) return true;
                if (_delegates.TryUpdate(name, entry.WithEnabled(enabled), entry))
                    return true;
            }
            return false;
        }

        /// <summary>获取原始委托（忽略禁用项）</summary>
        public bool TryGetDelegate(string name, out Delegate del)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (_delegates.TryGetValue(name, out var entry) && entry.Enabled)
            {
                del = entry.Delegate;
                return true;
            }
            del = null;
            return false;
        }

        /// <summary>调用无参数无返回值委托</summary>
        public void Invoke(string name)
        {
            if (!TryGetDelegate(name, out var del))
                throw new InvalidOperationException($"委托 '{name}' 未启用。");

            // 1. 优先尝试高效率的直接转换调用
            if (del is Action action)
            {
                action();
                return;
            }

            // 2. 防御性回退：使用 DynamicInvoke，兼容任意无参委托
            try
            {
                del.DynamicInvoke();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"调用委托 '{name}' 失败。", ex);
            }
        }

        /// <summary>调用无参数有返回值委托</summary>
        public TResult Invoke<TResult>(string name)
        {
            if (!TryGetDelegate(name, out var del))
                throw new InvalidOperationException($"委托 '{name}' 未启用。");

            // 1. 优先尝试高效率的直接转换调用
            if (del is Func<TResult> func)
                return func();

            // 2. 防御性回退：使用 DynamicInvoke
            try
            {
                return (TResult)del.DynamicInvoke();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"调用委托 '{name}' 失败。", ex);
            }
        }

        /// <summary>调用带参委托</summary>
        public void Invoke<T>(string name, T arg)
        {
            if (!TryGetDelegate(name, out var del))
                throw new InvalidOperationException($"委托 '{name}' 未启用。");

            if (del is Action<T> action)
            {
                action(arg);
                return;
            }

            // 防御性回退
            try
            {
                del.DynamicInvoke(arg);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"调用委托 '{name}' 失败。", ex);
            }
        }

        /// <summary>调用带参并有返回值的委托</summary>
        public TResult Invoke<T, TResult>(string name, T arg)
        {
            if (!TryGetDelegate(name, out var del))
                throw new InvalidOperationException($"委托 '{name}' 未启用。");

            if (del is Func<T, TResult> func)
                return func(arg);

            // 防御性回退
            try
            {
                return (TResult)del.DynamicInvoke(arg);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"调用委托 '{name}' 失败。", ex);
            }
        }

        /// <summary>获取所有已启用委托的名称集合</summary>
        public ICollection<string> EnabledNames()
        {
            return _delegates.Where(kvp => kvp.Value.Enabled)
                             .Select(kvp => kvp.Key)
                             .ToList();
        }

        /// <summary>移除委托（无论状态）</summary>
        public bool Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _delegates.TryRemove(name, out _);
        }

        /// <summary>清空所有委托</summary>
        public void Clear() => _delegates.Clear();

        /// <summary>检查委托名是否物理存在</summary>
        public bool Exists(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _delegates.ContainsKey(name);
        }

        /// <summary>检查委托是否已启用</summary>
        public bool IsEnabled(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _delegates.TryGetValue(name, out var entry) && entry.Enabled;
        }
    }
}
