namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusCancellationTests
{
    [Fact]
    public async Task PreCanceled_NoHandlersInvoked_IsCanceled()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<OtherTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent(), cts.Token);

        Assert.True(result.IsCanceled);
        Assert.Equal(0, count);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(0, result.ExecutedCount);
    }

    [Fact]
    public async Task MidFlightCancellation_HandlerReceivesToken_IsCanceled()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<OtherTestEvent>(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            count++;
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent(), cts.Token);

        Assert.True(result.IsCanceled);
        Assert.Equal(1, result.CanceledCount);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task HandlerThrowsOceWithOwnToken_IsError()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent>((_, _) =>
            throw new OperationCanceledException(new CancellationToken(canceled: true)));

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.False(result.IsCanceled);
        Assert.Equal(1, result.FailedCount);
        Assert.IsType<OperationCanceledException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task Sequential_Cancellation_StopsRemaining()
    {
        var bus = new EventBus(new EventBusOptions { ExecutionMode = EventExecutionMode.Sequential });
        using var cts = new CancellationTokenSource();
        int secondCount = 0;

        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            cts.Cancel();
            return Task.CompletedTask;
        }, order: -1);
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            secondCount++;
            return Task.CompletedTask;
        }, order: 1);

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent(), cts.Token);

        Assert.True(result.IsCanceled);
        Assert.Equal(0, secondCount);
        Assert.Equal(1, result.ExecutedCount);
        Assert.Equal(2, result.MatchedCount);
    }

    [Fact]
    public async Task Parallel_Cancellation_AwaitsInFlight()
    {
        var bus = new EventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        int completed = 0;

        bus.Subscribe<OtherTestEvent>(async (_, cancellationToken) =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await Task.Delay(50);
                completed++;
                throw;
            }
        });

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent(), cts.Token);

        Assert.True(result.IsCanceled);
        Assert.Equal(1, completed);
    }

    [Fact]
    public async Task CancellationPlusHandlerError_ThrowOnError_StillAggregates()
    {
        var bus = new EventBus(new EventBusOptions { ExecutionMode = EventExecutionMode.Sequential });
        using var cts = new CancellationTokenSource();

        bus.Subscribe<OtherTestEvent>((_, _) => throw new InvalidOperationException("boom"), order: -1);
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            cts.Cancel();
            return Task.CompletedTask;
        }, order: 1);

        EventDispatchException exception = await Assert.ThrowsAsync<EventDispatchException>(
            () => bus.PublishAsync(new OtherTestEvent(), cts.Token));

        Assert.Equal(1, exception.Result.FailedCount);
        Assert.True(exception.Result.IsCanceled);
    }
}
