using ReluctersteSDK.DataStructures.Interfaces;
using ReluctersteSDK.DataStructures.Structures;

namespace ReluctersteSDK.DataStructures.Test;

public class NodeBaseTests
{
    private sealed class TestNode : NodeBase
    {
        public TestNode(string? name = null) : base(name) { }

        public List<string> Log { get; } = new();

        protected override void OnConnected(IXNode target) => Log.Add($"out:{target}");
        protected override void OnReceivedConnection(IXNode source) => Log.Add($"in:{source}");
        protected override void OnDisconnected(IXNode target) => Log.Add($"unout:{target}");
        protected override void OnLostConnection(IXNode source) => Log.Add($"unin:{source}");
    }

    /// <summary>不继承 NodeBase 的第三方 IXNode 实现，用于验证拒绝逻辑。</summary>
    private sealed class ForeignNode : IXNode
    {
        public IReadOnlyList<IXNode> Inputs => Array.Empty<IXNode>();
        public IReadOnlyList<IXNode> Outputs => Array.Empty<IXNode>();
        public bool ConnectTo(IXNode target) => false;
        public bool DisconnectFrom(IXNode target) => false;
        public void ClearConnections() { }
        public bool CanConnectTo(IXNode target) => false;
    }

    [Fact]
    public void ConnectTo_Registers_Both_Sides()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");

        Assert.True(a.ConnectTo(b));

        Assert.Contains(b, a.Outputs);
        Assert.Contains(a, b.Inputs);
        Assert.Equal(1, a.OutputCount);
        Assert.Equal(1, b.InputCount);
    }

    [Fact]
    public void ConnectTo_Rejects_Self()
    {
        var a = new TestNode("A");

        Assert.False(a.ConnectTo(a));
        Assert.Empty(a.Outputs);
        Assert.Empty(a.Inputs);
    }

    [Fact]
    public void ConnectTo_Rejects_Duplicate()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");

        Assert.True(a.ConnectTo(b));
        Assert.False(a.ConnectTo(b));
        Assert.Single(a.Outputs);
    }

    [Fact]
    public void ConnectTo_Rejects_Foreign_Implementation()
    {
        var a = new TestNode("A");
        var foreign = new ForeignNode();

        Assert.False(a.CanConnectTo(foreign));
        Assert.False(a.ConnectTo(foreign));
    }

    [Fact]
    public void DisconnectFrom_Removes_Both_Sides()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");
        a.ConnectTo(b);

        Assert.True(a.DisconnectFrom(b));

        Assert.Empty(a.Outputs);
        Assert.Empty(b.Inputs);
        Assert.False(a.DisconnectFrom(b));   // 已断开
    }

    [Fact]
    public void ClearConnections_Clears_Both_Directions()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");
        var c = new TestNode("C");

        a.ConnectTo(b);   // a → b
        c.ConnectTo(b);   // c → b
        b.ConnectTo(a);   // b → a（成环，默认允许）

        b.ClearConnections();

        Assert.Empty(b.Inputs);
        Assert.Empty(b.Outputs);
        Assert.Empty(a.Inputs);    // a 的输入来自 b
        Assert.Empty(c.Outputs);   // c 的输出指向 b
    }

    [Fact]
    public void Inputs_And_Outputs_Are_ReadOnly_Views()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");
        a.ConnectTo(b);

        Assert.IsNotType<List<IXNode>>(a.Outputs);
        Assert.IsNotType<List<IXNode>>(b.Inputs);

        Assert.False(a.Outputs is List<IXNode>);
        Assert.False(b.Inputs is List<IXNode>);
    }

    [Fact]
    public void WouldCreateCycle_Detects_Back_Edge()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");
        var c = new TestNode("C");

        a.ConnectTo(b);   // a → b
        b.ConnectTo(c);   // b → c

        Assert.True(c.WouldCreateCycle(a));      // c → a 会成环
        Assert.False(a.WouldCreateCycle(c));     // a → c 不会（本来就有路径 a→b→c，再加一条直连也不成环）
        Assert.False(a.WouldCreateCycle(new TestNode("D")));
    }

    [Fact]
    public void Hooks_Fire_After_Topology_Is_Consistent()
    {
        var a = new TestNode("A");
        var b = new TestNode("B");

        a.ConnectTo(b);

        Assert.Contains(b, a.Outputs);
        Assert.Contains(a, b.Inputs);
        Assert.Equal(2, a.Log.Count + b.Log.Count);
        Assert.StartsWith("out:", a.Log[0]);
        Assert.StartsWith("in:", b.Log[0]);
    }

    [Fact]
    public void Name_Cannot_Be_Null()
    {
        var a = new TestNode("A");

        Assert.Throws<ArgumentNullException>(() => a.Name = null!);
        Assert.Equal("A", a.Name);
    }

    [Fact]
    public void Name_Defaults_To_Type_Name()
    {
        var node = new TestNode();

        Assert.Equal(nameof(TestNode), node.Name);
        Assert.NotEqual(Guid.Empty, node.Id);
    }
}
