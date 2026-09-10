using ReluctersteSDK.DataStructures.Interfaces;
using ReluctersteSDK.DataStructures.Structures;
using ReluctersteSDK.MathTool.Vectors;
using System.Runtime.CompilerServices;

namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 场景中二维节点的抽象基类，作为场景图（Scene Graph）中可组合的基本单元。
    /// 节点携带 2D 变换（位置、旋转、缩放），支持树形父子关系，并约定更新与渲染的生命周期。
    /// </summary>
    public abstract class SceneNode2D : NodeBase
    {
        /// <summary>节点的坐标系位置，默认位于原点。</summary>
        public CartesianVector2D Position { get; set; } = CartesianVector2D.Zero;
        /// <summary>节点的旋转角度（单位：度），默认不旋转。</summary>
        public float Rotation { get; set; } = 0f;
        /// <summary>节点的缩放比例，默认不缩放。</summary>
        public CartesianVector2D Scale { get; set; } = CartesianVector2D.One;
        /// <summary>节点是否被激活。为 false 时 <see cref="Update"/> 将跳过自身及其子树。</summary>
        public bool IsActive { get; set; } = true;
        /// <summary>节点是否可见。为 false 时 <see cref="Render"/> 将跳过自身及其子树。</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>获取父节点；若节点当前没有父节点，则返回 null。</summary>
        public SceneNode2D? Parent => _inputs.Count > 0 ? _inputs[0] as SceneNode2D : null;
        /// <summary>获取子节点的只读列表。</summary>
        public IReadOnlyList<INode> Children => _outputs;

        /// <summary>
        /// 使用指定名称与标识初始化节点实例。
        /// </summary>
        /// <param name="name">节点名称，可选。</param>
        /// <param name="id">节点唯一标识，可选；为空时由基类生成。</param>
        protected SceneNode2D(string? name = null, Guid? id = null) : base(name, id) { }

        #region 拓扑树约束防御
        /// <summary>
        /// 判断当前节点能否将目标节点连接为子节点。
        /// 除基类约束外，还要求目标是 <see cref="SceneNode2D"/>、目标尚无父节点，且连接后不会形成环。
        /// </summary>
        /// <param name="target">待连接的目标节点。</param>
        /// <returns>可连接时返回 true，否则返回 false。</returns>
        public override bool CanConnectTo(INode target)
        {
            if (!base.CanConnectTo(target)) return false;
            if (target is not SceneNode2D child) return false;
            if (child.Inputs.Count > 0) return false;

            SceneNode2D? ancestor = this;
            while (ancestor != null)
            {
                if (ReferenceEquals(ancestor, child)) return false;
                ancestor = ancestor.Parent;
            }
            return true;
        }
        /// <summary>将指定节点添加为当前节点的子节点。</summary>
        /// <param name="child">要添加的子节点。</param>
        public void AddChild(SceneNode2D child) => ConnectTo(child);
        /// <summary>将指定节点从当前节点的子节点中移除。</summary>
        /// <param name="child">要移除的子节点。</param>
        public void RemoveChild(SceneNode2D child) => DisconnectFrom(child);
        #endregion

        #region 遍历与生命周期
        /// <summary>
        /// 以深度优先顺序更新自身及其子树。
        /// 当节点处于非激活状态时，将跳过自身与子树。
        /// </summary>
        /// <param name="deltaTime">上一次更新至本次更新之间的时间间隔（秒）。</param>
        public virtual void Update(float deltaTime)
        {
            if (!IsActive) return;
            OnUpdate(deltaTime);

            for (int i = 0; i < _outputs.Count; i++)
            {
                if (_outputs[i] is SceneNode2D child)
                    child.Update(deltaTime);
            }
        }
        /// <summary>
        /// 以深度优先顺序渲染自身及其子树。
        /// 当节点处于不可见状态时，将跳过自身与子树。
        /// </summary>
        /// <param name="renderContext">渲染上下文，用于向底层图形接口提交图元。</param>
        public virtual void Render(object renderContext)
        {
            if (!IsVisible) return;
            OnRender(renderContext);

            for (int i = 0; i < _outputs.Count; i++)
            {
                if (_outputs[i] is SceneNode2D child)
                    child.Render(renderContext);
            }
        }
        #endregion

        /// <summary>自身逻辑更新，由子类重写</summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>自身图元提交，由子类重写</summary>
        protected virtual void OnRender(object renderContext) { }
    }
}
