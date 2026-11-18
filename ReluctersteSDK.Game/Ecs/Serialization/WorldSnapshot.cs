using System.Text.Json;

namespace ReluctersteSDK.Game.Ecs.Serialization
{
    /// <summary>世界快照的 JSON DTO。</summary>
    public sealed class WorldSnapshot
    {
        /// <summary>快照格式版本。</summary>
        public int Version { get; set; } = 1;

        /// <summary>实体列表。</summary>
        public List<EntitySnapshot> Entities { get; set; } = new List<EntitySnapshot>();

        /// <summary>单例组件列表。</summary>
        public List<SingletonSnapshot> Singletons { get; set; } = new List<SingletonSnapshot>();
    }

    /// <summary>单个实体的快照。</summary>
    public sealed class EntitySnapshot
    {
        /// <summary>槽位索引。</summary>
        public int Index { get; set; }

        /// <summary>代数。</summary>
        public uint Generation { get; set; }

        /// <summary>组件列表。</summary>
        public List<ComponentSnapshot> Components { get; set; } = new List<ComponentSnapshot>();
    }

    /// <summary>单个组件的快照。</summary>
    public sealed class ComponentSnapshot
    {
        /// <summary>组件类型名（注册表中的键）。</summary>
        public string TypeName { get; set; } = "";

        /// <summary>组件数据。</summary>
        public JsonElement Data { get; set; }
    }

    /// <summary>单个单例组件的快照。</summary>
    public sealed class SingletonSnapshot
    {
        /// <summary>单例类型名（注册表中的键）。</summary>
        public string TypeName { get; set; } = "";

        /// <summary>数据。</summary>
        public JsonElement Data { get; set; }
    }

    /// <summary>快照捕获/恢复选项。</summary>
    public sealed class WorldSnapshotOptions
    {
        /// <summary>恢复前是否清空目标世界；默认 true。</summary>
        public bool ClearBeforeRestore { get; set; } = true;

        /// <summary>是否包含单例组件；默认 true。</summary>
        public bool IncludeSingletons { get; set; } = true;

        /// <summary>遇到未注册组件时是否抛异常；默认 true，设为 false 则跳过。</summary>
        public bool ThrowOnUnsupportedComponent { get; set; } = true;

        /// <summary>JSON 序列化选项。</summary>
        public JsonSerializerOptions JsonOptions { get; set; } = SnapshotJson.CreateOptions();
    }
}
