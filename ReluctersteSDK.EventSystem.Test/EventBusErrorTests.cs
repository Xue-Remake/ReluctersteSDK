using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusErrorTests
{
    [Fact]
    public async Task ThrowOnErrorTrue_ThrowsEventDispatchException()
    {
        var bus = new EventBus();
        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"));

        EventDispatchException exception = await Assert.ThrowsAsync<EventDispatchException>(
            () => bus.PublishAsync(new OtherTestEvent()));

        Assert.Single(exception.Errors);
        Assert.IsType<InvalidOperationException>(exception.Errors[0].Exception);
        Assert.Equal(1, exception.Result.FailedCount);
        Assert.False(exception.Result.IsSuccess);
    }

    [Fact]
    public async Task ThrowOnErrorFalse_ReturnsResultWithErrors()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Exception_InnerIsAggregateException()
    {
        var bus = new EventBus();
        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"));

        EventDispatchException exception = await Assert.ThrowsAsync<EventDispatchException>(
            () => bus.PublishAsync(new OtherTestEvent()));

        AggregateException aggregate = Assert.IsType<AggregateException>(exception.InnerException);
        Assert.Single(aggregate.InnerExceptions);
    }

    [Fact]
    public async Task OnError_CalledOncePerError()
    {
        int callbackCount = 0;
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnError = false,
            OnError = _ => callbackCount++,
        });

        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("first"));
        bus.Subscribe<OtherTestEvent>((_, _) => throw new ArgumentException("second"));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, callbackCount);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task OnError_Throws_CollectedInCallbackErrors()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnError = false,
            OnError = _ => throw new InvalidOperationException("callback"),
        });

        bus.Subscribe<OtherTestEvent>((_, _) => throw new ArgumentException("handler"));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Single(result.Errors);
        Assert.Single(result.CallbackErrors);
        Assert.IsType<InvalidOperationException>(result.CallbackErrors[0]);
    }

    [Fact]
    public async Task HandlerThrowsSynchronously_Captured()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("sync"));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.IsType<InvalidOperationException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task HandlerReturnsNullTask_Captured()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent>((_, _) => null!);

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.IsType<InvalidOperationException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task DiResolutionFailure_Captured()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ThrowOnError = false,
            EnableActivatorFallback = false,
        });
        bus.Subscribe<OtherTestEvent, NoOpHandler>();

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(typeof(NoOpHandler), result.Errors[0].HandlerType);
        Assert.IsType<InvalidOperationException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task SuccessAndFailureCounts_AreCorrect()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent>((_, _) => Task.CompletedTask);
        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"));
        bus.Subscribe<OtherTestEvent>((_, _) => Task.CompletedTask);

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(3, result.MatchedCount);
        Assert.Equal(3, result.ExecutedCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task Result_EventId_MatchesEvent()
    {
        var bus = new EventBus();
        var evt = new OtherTestEvent();

        EventDispatchResult result = await bus.PublishAsync(evt);

        Assert.Equal(evt.EventId, result.EventId);
        Assert.Equal(typeof(OtherTestEvent), result.EventType);
    }
}

/// <summary>构造函数总是抛异常的处理器，用于 DI 解析失败测试。</summary>
public sealed class ThrowingConstructorHandler : IEventHandler<OtherTestEvent>
{
    public ThrowingConstructorHandler()
        => throw new InvalidOperationException("ctor");

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
