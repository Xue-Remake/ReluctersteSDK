using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusTimeoutTests
{
    [Fact]
    public async Task HandlerIgnoresToken_ReturnsTimedOut()
    {
        var bus = new EventBus(new EventBusOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(50),
            ThrowOnError = false,
        });
        bus.Subscribe<OtherTestEvent>(async (_, _) => await Task.Delay(2000));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.True(result.IsTimedOut);
        Assert.Equal(1, result.TimedOutCount);
        Assert.Single(result.Errors);
        Assert.True(result.Errors[0].IsTimeout);
        Assert.IsType<TimeoutException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task HandlerObservesLinkedToken_CompletesAsTimedOut()
    {
        var bus = new EventBus(new EventBusOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(50),
            ThrowOnError = false,
        });
        bus.Subscribe<OtherTestEvent>(async (_, cancellationToken) =>
            await Task.Delay(Timeout.Infinite, cancellationToken));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.True(result.IsTimedOut);
        Assert.Equal(1, result.TimedOutCount);
        Assert.True(result.Errors[0].IsTimeout);
    }

    [Fact]
    public async Task HandlerOwnTimeoutException_IsError_NotBusTimeout()
    {
        var bus = new EventBus(new EventBusOptions
        {
            HandlerTimeout = TimeSpan.FromSeconds(1),
            ThrowOnError = false,
        });
        bus.Subscribe<OtherTestEvent>((_, _) => throw new TimeoutException("own"));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.False(result.IsTimedOut);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.Errors[0].IsTimeout);
        Assert.IsType<TimeoutException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task Sequential_Timeout_StopsRemaining()
    {
        var bus = new EventBus(new EventBusOptions
        {
            ExecutionMode = EventExecutionMode.Sequential,
            HandlerTimeout = TimeSpan.FromMilliseconds(50),
            ThrowOnError = false,
        });
        int secondCount = 0;

        bus.Subscribe<OtherTestEvent>(async (_, _) => await Task.Delay(2000), order: -1);
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            secondCount++;
            return Task.CompletedTask;
        }, order: 1);

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.True(result.IsTimedOut);
        Assert.Equal(0, secondCount);
        Assert.Equal(1, result.ExecutedCount);
        Assert.Equal(2, result.MatchedCount);
    }

    [Fact]
    public async Task Timeout_DoesNotAffectOtherParallelHandlers()
    {
        var bus = new EventBus(new EventBusOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(250),
            ThrowOnError = false,
        });
        int successCount = 0;

        bus.Subscribe<OtherTestEvent>(async (_, _) => await Task.Delay(2000));
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            successCount++;
            return Task.CompletedTask;
        });

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.TimedOutCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task Timeout_ScopeDisposedOnlyAfterHandlerCompletes()
    {
        var tracker = new SlowDisposableTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddScoped<SlowDisposableHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider, new EventBusOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(50),
            ThrowOnError = false,
        });
        bus.Subscribe<OtherTestEvent, SlowDisposableHandler>();

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());
        Assert.True(result.IsTimedOut);
        Assert.False(tracker.Disposed);

        tracker.Release.TrySetResult();

        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!tracker.Disposed && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        Assert.True(tracker.Disposed);
    }
}

/// <summary>慢处理器与作用域释放状态的共享记录。</summary>
public sealed class SlowDisposableTracker
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public volatile bool Disposed;
}

/// <summary>忽略取消令牌的慢处理器，用于验证 Scope 不会提前释放。</summary>
public sealed class SlowDisposableHandler : IEventHandler<OtherTestEvent>, IAsyncDisposable
{
    private readonly SlowDisposableTracker _tracker;

    public SlowDisposableHandler(SlowDisposableTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        _tracker.Started.TrySetResult();
        await _tracker.Release.Task;
    }

    public ValueTask DisposeAsync()
    {
        _tracker.Disposed = true;
        return ValueTask.CompletedTask;
    }
}
