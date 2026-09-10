using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 空容器节点，仅用于组织层级或充当局部 Root
    /// </summary>
    public class ContainerNode2D : SceneNode2D
    {
        /// <summary>使用指定名称初始化一个空容器节点。</summary>
        /// <param name="name">容器节点名称。</param>
        public ContainerNode2D(string name) : base(name) { }
    }
}
