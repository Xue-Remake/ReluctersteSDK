namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 2D 场景的根对象，持有场景名称与根节点，负责在顶层维护节点树并驱动整体更新与渲染。
    /// </summary>
    public class Scene2D
    {
        /// <summary>场景名称，用于标识与调试。</summary>
        public string Name { get; set; }
        /// <summary>场景的根节点，所有场景对象均挂载其下。</summary>
        public SceneNode2D Root { get; }
        /// <summary>
        /// 使用指定名称初始化一个新场景，并为根节点创建默认的容器节点。
        /// </summary>
        /// <param name="name">场景名称，默认 "Scene2D"。</param>
        public Scene2D(string name = "Scene2D")
        {
            Name = name;
            Root = new ContainerNode2D("SceneRoot");
        }

        /// <summary>将指定节点添加到根节点下。</summary>
        /// <param name="node">要添加的节点。</param>
        public void Add(SceneNode2D node) => Root.AddChild(node);
        /// <summary>将指定节点从根节点下移除。</summary>
        /// <param name="node">要移除的节点。</param>
        public void Remove(SceneNode2D node) => Root.RemoveChild(node);

        /// <summary>
        /// 更新整个场景树。
        /// </summary>
        /// <param name="deltaTime">上一次更新至本次更新之间的时间间隔（秒）。</param>
        public void Update(float deltaTime)
        {
            Root.Update(deltaTime);
        }
        /// <summary>
        /// 渲染整个场景树。
        /// </summary>
        /// <param name="renderContext">渲染上下文，用于向底层图形接口提交图元。</param>
        public void Render(object renderContext)
        {
            Root.Render(renderContext);
        }
    }
}
