namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusConcurrencyTests
{
    [Fact]
    public async Task ConcurrentSubscribeAndPublish_NoLostSubscriptions()
    {
        var bus = new EventBus();
        int count = 0;

        Task<IDisposable>[] tasks = Enumerable.Range(0, 200)
            .Select(_ => Task.Run(() => bus.Subscribe<OtherTestEvent>((_, _) =>
            {
                Interlocked.Increment(ref count);
                return Task.CompletedTask;
            })))
            .ToArray();

        IDisposable[] tokens = await Task.WhenAll(tasks);
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(200, count);

        foreach (IDisposable token in tokens)
            token.Dispose();
    }

    [Fact]
    public async Task ConcurrentDisposeAndPublish_NoExceptions()
    {
        var bus = new EventBus();
        IDisposable[] tokens = Enumerable.Range(0, 100)
            .Select(_ => bus.Subscribe<OtherTestEvent>((_, _) => Task.CompletedTask))
            .ToArray();

        var operations = new List<Task>();
        operations.AddRange(tokens.Select(token => Task.Run(token.Dispose)));
        operations.AddRange(Enumerable.Range(0, 20).Select(_ => bus.PublishAsync(new OtherTestEvent())));

        await Task.WhenAll(operations);
    }

    [Fact]
    public async Task ManyParallelPublishes_HandlerInvokedExpectedCount()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => bus.PublishAsync(new OtherTestEvent())));

        Assert.Equal(20, count);
    }

    [Fact]
    public async Task ConcurrentClearAndPublish_NoExceptions()
    {
        var bus = new EventBus();
        for (int i = 0; i < 50; i++)
            bus.Subscribe<OtherTestEvent>((_, _) => Task.CompletedTask);

        var operations = new List<Task>();
        operations.AddRange(Enumerable.Range(0, 10).Select(_ => Task.Run(bus.Clear)));
        operations.AddRange(Enumerable.Range(0, 20).Select(_ => bus.PublishAsync(new OtherTestEvent())));

        await Task.WhenAll(operations);
    }

    [Fact]
    public async Task ReentrantPublishUnderConcurrency_NoDeadlock()
    {
        var bus = new EventBus();
        int innerCount = 0;

        bus.Subscribe<OtherTestEvent>((_, _) => bus.PublishAsync(new InnerTestEvent()));
        bus.Subscribe<InnerTestEvent>((_, _) =>
        {
            Interlocked.Increment(ref innerCount);
            return Task.CompletedTask;
        });

        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => bus.PublishAsync(new OtherTestEvent())));

        Assert.Equal(20, innerCount);
    }

    [Fact]
    public async Task ConcurrentDispose_SameHandler_TokenIdentityCorrect()
    {
        var bus = new EventBus();
        int count = 0;
        IDisposable[] tokens = Enumerable.Range(0, 100)
            .Select(_ => bus.Subscribe<OtherTestEvent>((_, _) =>
            {
                Interlocked.Increment(ref count);
                return Task.CompletedTask;
            }))
            .ToArray();

        await Task.WhenAll(tokens
            .Where((_, index) => index % 2 == 0)
            .Select(token => Task.Run(token.Dispose)));

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(50, count);
    }
}
