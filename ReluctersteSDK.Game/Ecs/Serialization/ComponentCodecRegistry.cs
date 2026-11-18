using System.Text.Json;

namespace ReluctersteSDK.Game.Ecs.Serialization
{
    internal interface IComponentCodec
    {
        Type ComponentType { get; }
        string TypeName { get; }
        bool Has(World world, Entity entity);
        JsonElement Write(World world, Entity entity, JsonSerializerOptions options);
        void Read(World world, Entity entity, JsonElement data, JsonSerializerOptions options);
        bool HasSingleton(World world);
        JsonElement WriteSingleton(World world, JsonSerializerOptions options);
        void ReadSingleton(World world, JsonElement data, JsonSerializerOptions options);
    }

    internal sealed class JsonComponentCodec<T> : IComponentCodec where T : notnull
    {
        public JsonComponentCodec(string typeName)
        {
            TypeName = typeName;
        }

        public Type ComponentType => typeof(T);
        public string TypeName { get; }

        public bool Has(World world, Entity entity) => world.Has<T>(entity);

        public JsonElement Write(World world, Entity entity, JsonSerializerOptions options)
            => JsonSerializer.SerializeToElement(world.Get<T>(entity), options);

        public void Read(World world, Entity entity, JsonElement data, JsonSerializerOptions options)
        {
            T value = data.Deserialize<T>(options)
                ?? throw new JsonException($"无法反序列化组件 {typeof(T).FullName}。");
            world.Add(entity, value);
        }

        public bool HasSingleton(World world) => world.HasSingleton<T>();

        public JsonElement WriteSingleton(World world, JsonSerializerOptions options)
            => JsonSerializer.SerializeToElement(world.GetSingleton<T>(), options);

        public void ReadSingleton(World world, JsonElement data, JsonSerializerOptions options)
        {
            T value = data.Deserialize<T>(options)
                ?? throw new JsonException($"无法反序列化单例 {typeof(T).FullName}。");
            world.SetSingleton(value);
        }
    }

    /// <summary>
    /// 快照编解码注册表：只有注册过的类型会参与序列化。
    /// </summary>
    public sealed class ComponentCodecRegistry
    {
        private readonly Dictionary<string, IComponentCodec> _byName =
            new Dictionary<string, IComponentCodec>(StringComparer.Ordinal);
        private readonly Dictionary<Type, IComponentCodec> _byType = new Dictionary<Type, IComponentCodec>();

        /// <summary>注册一个使用 System.Text.Json 的组件类型。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="typeName">可选的类型名；默认使用类型全名。</param>
        public void Register<T>(string? typeName = null) where T : notnull
        {
            string name = typeName ?? typeof(T).FullName ?? typeof(T).Name;
            var codec = new JsonComponentCodec<T>(name);
            _byName[name] = codec;
            _byType[typeof(T)] = codec;
        }

        /// <summary>注册一个使用 System.Text.Json 的组件类型（<see cref="Register{T}"/> 的别名）。</summary>
        public void RegisterJson<T>(string? typeName = null) where T : notnull => Register<T>(typeName);

        /// <summary>已注册的组件类型集合。</summary>
        public IReadOnlyCollection<Type> RegisteredTypes => _byType.Keys;

        internal bool TryGet(string typeName, out IComponentCodec codec)
            => _byName.TryGetValue(typeName, out codec!);

        internal bool TryGet(Type type, out IComponentCodec codec)
            => _byType.TryGetValue(type, out codec!);

        /// <summary>创建包含基础组件（Transform/Parent/Velocity/Acceleration/Damping/Heading）的默认注册表。</summary>
        public static ComponentCodecRegistry CreateDefault()
        {
            var registry = new ComponentCodecRegistry();
            registry.Register<Transform>();
            registry.Register<Parent>();
            registry.Register<Velocity>();
            registry.Register<Acceleration>();
            registry.Register<Damping>();
            registry.Register<Heading>();
            return registry;
        }
    }
}
