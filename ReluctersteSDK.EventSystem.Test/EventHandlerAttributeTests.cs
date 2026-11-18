using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventHandlerAttributeTests
{
    [Fact]
    public async Task ScansPublicAndPrivateMethods_AndSupportedReturnTypes()
    {
        var target = new ValidMethodHandlers();
        var bus = new EventBus();
        using IDisposable token = bus.SubscribeFromTarget(target);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, target.PrivateCount);
        Assert.Equal(1, target.TaskCount);
        Assert.Equal(1, target.ValueTaskCount);
    }

    [Fact]
    public async Task Order_Applied()
    {
        var target = new OrderedHandlers();
        var bus = new EventBus(new EventBusOptions { ExecutionMode = EventExecutionMode.Sequential });
        using IDisposable token = bus.SubscribeFromTarget(target);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(new[] { "first", "second" }, target.Order);
    }

    [Fact]
    public async Task AttributeOrderFallsBackToParameter()
    {
        var order = new List<string>();
        var target = new UnorderedHandlers(order);
        var bus = new EventBus(new EventBusOptions { ExecutionMode = EventExecutionMode.Sequential });

        bus.Subscribe<OtherTestEvent>((_, _) => { order.Add("manual"); return Task.CompletedTask; }, order: 0);
        using IDisposable token = bus.SubscribeFromTarget(target, order: 5);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(new[] { "manual", "scanned" }, order);
    }

    [Fact]
    public async Task CancellationTokenParameter_ReceivesPublishToken()
    {
        var target = new TokenHandler();
        var bus = new EventBus();
        using IDisposable token = bus.SubscribeFromTarget(target);

        using var cts = new CancellationTokenSource();
        await bus.PublishAsync(new OtherTestEvent(), cts.Token);

        Assert.Equal(cts.Token, target.Observed);
    }

    [Fact]
    public void RejectsAsyncVoid()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new AsyncVoidHandler()));
    }

    [Fact]
    public void RejectsGenericMethod()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new GenericHandler()));
    }

    [Fact]
    public void RejectsStaticMethod()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new StaticHandler()));
    }

    [Fact]
    public void RejectsRefParameter()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new RefHandler()));
    }

    [Fact]
    public void RejectsParamsParameter()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new ParamsHandler()));
    }

    [Fact]
    public void RejectsTooManyParameters()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new ThreeParameterHandler()));
    }

    [Fact]
    public void RejectsNonEventParameter()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new NonEventHandler()));
    }

    [Fact]
    public void RejectsTaskOfT()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new TaskOfTHandler()));
    }

    [Fact]
    public void RejectsInterfaceEventWhenDisabled()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new InterfaceEventParameterHandler()));
    }

    [Fact]
    public void RejectsIEventParameter()
    {
        var bus = new EventBus();
        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(new IEventParameterHandler()));
    }

    [Fact]
    public async Task InvalidMethod_DoesNotLeavePartialRegistration()
    {
        var target = new PartialHandler();
        var bus = new EventBus();

        Assert.Throws<InvalidOperationException>(() => bus.SubscribeFromTarget(target));
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(0, target.Count);
    }

    [Fact]
    public async Task InheritedAttribute_OnOverride_IsFound()
    {
        var target = new DerivedInheritedHandler();
        var bus = new EventBus();
        using IDisposable token = bus.SubscribeFromTarget(target);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, target.Count);
    }

    [Fact]
    public async Task MethodThrows_UnwrappedAsHandlerError()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        using IDisposable token = bus.SubscribeFromTarget(new ThrowingMethodHandler());

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.IsType<InvalidOperationException>(result.Errors[0].Exception);
    }

    [Fact]
    public void NoAttributedMethods_ReturnsNoOpToken()
    {
        var bus = new EventBus();
        using IDisposable token = bus.SubscribeFromTarget(new object());
    }

    [Fact]
    public void NullTarget_Throws()
    {
        var bus = new EventBus();
        Assert.Throws<ArgumentNullException>(() => bus.SubscribeFromTarget(null!));
    }

    [Fact]
    public void ValueTypeTarget_Throws()
    {
        var bus = new EventBus();
        Assert.Throws<ArgumentException>(() => bus.SubscribeFromTarget(new StructTarget()));
    }

    [Fact]
    public async Task Overloads_AreBothRegistered()
    {
        var target = new OverloadHandler();
        var bus = new EventBus();
        using IDisposable token = bus.SubscribeFromTarget(target);

        await bus.PublishAsync(new DerivedTestEvent());

        Assert.Equal(2, target.Count);
    }

    [Fact]
    public async Task DisposingCompositeToken_RemovesAll()
    {
        var target = new ValidMethodHandlers();
        var bus = new EventBus();
        IDisposable token = bus.SubscribeFromTarget(target);

        token.Dispose();
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(0, target.PrivateCount);
        Assert.Equal(0, target.TaskCount);
        Assert.Equal(0, target.ValueTaskCount);
    }
}

public sealed class ValidMethodHandlers
{
    public int PrivateCount;
    public int TaskCount;
    public int ValueTaskCount;

    [EventHandler]
    private void HandlePrivate(OtherTestEvent @event) => PrivateCount++;

    [EventHandler]
    public Task HandleTask(OtherTestEvent @event)
    {
        TaskCount++;
        return Task.CompletedTask;
    }

    [EventHandler]
    public ValueTask HandleValueTask(OtherTestEvent @event)
    {
        ValueTaskCount++;
        return ValueTask.CompletedTask;
    }
}

public sealed class OrderedHandlers
{
    public List<string> Order { get; } = new();

    [EventHandler(Order = -1)]
    private void First(OtherTestEvent @event) => Order.Add("first");

    [EventHandler(Order = 1)]
    private void Second(OtherTestEvent @event) => Order.Add("second");
}

public sealed class UnorderedHandlers
{
    private readonly List<string> _order;

    public UnorderedHandlers(List<string> order) => _order = order;

    [EventHandler]
    private void Handle(OtherTestEvent @event) => _order.Add("scanned");
}

public sealed class TokenHandler
{
    public CancellationToken Observed { get; private set; }

    [EventHandler]
    private void Handle(OtherTestEvent @event, CancellationToken cancellationToken)
        => Observed = cancellationToken;
}

public sealed class AsyncVoidHandler
{
    [EventHandler]
    private async void Handle(OtherTestEvent @event)
        => await Task.Yield();
}

public sealed class GenericHandler
{
    [EventHandler]
    private void Handle<T>(OtherTestEvent @event) { }
}

public sealed class StaticHandler
{
    [EventHandler]
    private static void Handle(OtherTestEvent @event) { }
}

public sealed class RefHandler
{
    [EventHandler]
    private void Handle(ref OtherTestEvent @event) { }
}

public sealed class ParamsHandler
{
    [EventHandler]
    private void Handle(params OtherTestEvent[] events) { }
}

public sealed class ThreeParameterHandler
{
    [EventHandler]
    private void Handle(OtherTestEvent @event, CancellationToken cancellationToken, int extra) { }
}

public sealed class NonEventHandler
{
    [EventHandler]
    private void Handle(int value) { }
}

public sealed class TaskOfTHandler
{
    [EventHandler]
    private Task<int> Handle(OtherTestEvent @event) => Task.FromResult(1);
}

public sealed class InterfaceEventParameterHandler
{
    [EventHandler]
    private void Handle(IMarkedTestEvent @event) { }
}

public sealed class IEventParameterHandler
{
    [EventHandler]
    private void Handle(IEvent @event) { }
}

public sealed class PartialHandler
{
    public int Count;

    [EventHandler]
    private void Good(OtherTestEvent @event) => Count++;

    [EventHandler]
    private void Bad(int value) { }
}

public class BaseInheritedHandler
{
    public int Count;

    [EventHandler]
    public virtual void Handle(OtherTestEvent @event) => Count++;
}

public sealed class DerivedInheritedHandler : BaseInheritedHandler
{
    public override void Handle(OtherTestEvent @event) => Count++;
}

public sealed class ThrowingMethodHandler
{
    [EventHandler]
    private void Handle(OtherTestEvent @event)
        => throw new InvalidOperationException("boom");
}

public struct StructTarget
{
    [EventHandler]
    private void Handle(OtherTestEvent @event) { }
}

public sealed class OverloadHandler
{
    public int Count;

    [EventHandler]
    private void Handle(DerivedTestEvent @event) => Count++;

    [EventHandler]
    private void Handle(BaseTestEvent @event) => Count++;
}
