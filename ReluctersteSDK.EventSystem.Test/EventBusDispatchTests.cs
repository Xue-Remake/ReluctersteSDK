namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusDispatchTests
{
    [Fact]
    public async Task DerivedEvent_InvokesBaseSubscription()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<BaseTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        await bus.PublishAsync(new DerivedTestEvent());

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MultiLevelBaseChain_InvokesAllBaseHandlers()
    {
        var bus = new EventBus();
        int baseCount = 0;
        int derivedCount = 0;

        bus.Subscribe<BaseTestEvent>((_, _) => { baseCount++; return Task.CompletedTask; });
        bus.Subscribe<DerivedTestEvent>((_, _) => { derivedCount++; return Task.CompletedTask; });

        await bus.PublishAsync(new LeafTestEvent());

        Assert.Equal(1, baseCount);
        Assert.Equal(1, derivedCount);
    }

    [Fact]
    public async Task IncludeBaseTypesFalse_DoesNotInvokeBase()
    {
        var bus = new EventBus(new EventBusOptions { IncludeBaseTypes = false });
        int count = 0;
        bus.Subscribe<BaseTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        await bus.PublishAsync(new DerivedTestEvent());

        Assert.Equal(0, count);
    }

    [Fact]
    public void InterfaceSubscription_ThrowsWhenDisabled()
    {
        var bus = new EventBus();

        Assert.Throws<InvalidOperationException>(() =>
            bus.Subscribe<IMarkedTestEvent>((_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task IncludeInterfacesTrue_InvokesInterfaceHandler()
    {
        var bus = new EventBus(new EventBusOptions { IncludeInterfaces = true });
        int count = 0;
        bus.Subscribe<IMarkedTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        await bus.PublishAsync(new MarkedTestEvent());

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InterfaceDispatch_WorksWhenBaseTypesDisabled()
    {
        var bus = new EventBus(new EventBusOptions
        {
            IncludeInterfaces = true,
            IncludeBaseTypes = false,
        });
        int count = 0;
        bus.Subscribe<IMarkedTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        await bus.PublishAsync(new MarkedTestEvent());

        Assert.Equal(1, count);
    }

    [Fact]
    public void Subscribe_IEvent_Throws()
    {
        var bus = new EventBus(new EventBusOptions { IncludeInterfaces = true });

        Assert.Throws<InvalidOperationException>(() =>
            bus.Subscribe<Interfaces.IEvent>((_, _) => Task.CompletedTask));
    }

    [Fact]
    public void Subscribe_AbstractEvent_ThrowsWhenBaseTypesDisabled()
    {
        var bus = new EventBus(new EventBusOptions { IncludeBaseTypes = false });

        Assert.Throws<InvalidOperationException>(() =>
            bus.Subscribe<AbstractTestEvent>((_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task Sequential_ExecutesInOrder()
    {
        var bus = new EventBus(new EventBusOptions { ExecutionMode = EventExecutionMode.Sequential });
        var order = new List<string>();

        bus.Subscribe<OtherTestEvent>((_, _) => { order.Add("second"); return Task.CompletedTask; }, order: 10);
        bus.Subscribe<OtherTestEvent>((_, _) => { order.Add("first"); return Task.CompletedTask; }, order: -10);
        bus.Subscribe<OtherTestEvent>((_, _) => { order.Add("same-order-later"); return Task.CompletedTask; }, order: 0);
        bus.Subscribe<OtherTestEvent>((_, _) => { order.Add("same-order-earlier"); return Task.CompletedTask; }, order: 0);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(new[] { "first", "same-order-later", "same-order-earlier", "second" }, order);
    }

    [Fact]
    public async Task Sequential_ContinuesAfterHandlerError()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ExecutionMode = EventExecutionMode.Sequential,
            ThrowOnError = false,
        });
        int secondCount = 0;

        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"), order: -1);
        bus.Subscribe<OtherTestEvent>((_, _) => { secondCount++; return Task.CompletedTask; }, order: 1);

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, secondCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task Parallel_HandlersRunConcurrently()
    {
        var bus = new EventBus();
        int arrived = 0;
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<OtherTestEvent, CancellationToken, Task> handler = async (_, _) =>
        {
            if (Interlocked.Increment(ref arrived) == 2)
                bothStarted.TrySetResult();

            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        };

        bus.Subscribe(handler);
        bus.Subscribe(handler);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, arrived);
    }

    [Fact]
    public async Task Parallel_OneFailure_DoesNotPreventOthers()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        int successCount = 0;

        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"));
        bus.Subscribe<OtherTestEvent>((_, _) => { successCount++; return Task.CompletedTask; });

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, successCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task StructEvent_CanBePublished()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<StructTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        var evt = new StructTestEvent { EventId = Guid.NewGuid(), Timestamp = DateTime.UtcNow };
        EventDispatchResult result = await bus.PublishAsync(evt);

        Assert.Equal(1, count);
        Assert.Equal(evt.EventId, result.EventId);
    }
}
