using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusCoreTests
{
    [Fact]
    public async Task Publish_NoSubscribers_ReturnsSuccess()
    {
        var bus = new EventBus();

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(0, result.MatchedCount);
        Assert.Equal(0, result.ExecutedCount);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Publish_ExactType_InvokesOnce()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            count++;
            return Task.CompletedTask;
        });

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, count);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SubscribeAsync_And_SubscribeSync_Work()
    {
        var bus = new EventBus();
        int asyncCount = 0;
        int syncCount = 0;

        bus.SubscribeAsync<OtherTestEvent>(_ =>
        {
            asyncCount++;
            return Task.CompletedTask;
        });
        bus.SubscribeSync<OtherTestEvent>(_ => syncCount++);

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, asyncCount);
        Assert.Equal(1, syncCount);
    }

    [Fact]
    public void SubscribeSync_RejectsAsyncVoidDelegate()
    {
        var bus = new EventBus();
        Action<OtherTestEvent> handler = async _ => await Task.Yield();

        Assert.Throws<ArgumentException>(() => bus.SubscribeSync(handler));
    }

    [Fact]
    public async Task DisposeToken_Unsubscribes_AndIsIdempotent()
    {
        var bus = new EventBus();
        int count = 0;
        IDisposable token = bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            count++;
            return Task.CompletedTask;
        });

        token.Dispose();
        token.Dispose();
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DuplicateSubscribe_InvokesTwice_TokensIndependent()
    {
        var bus = new EventBus();
        int count = 0;
        Func<OtherTestEvent, CancellationToken, Task> handler = (_, _) =>
        {
            count++;
            return Task.CompletedTask;
        };

        IDisposable first = bus.Subscribe(handler);
        IDisposable second = bus.Subscribe(handler);

        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(2, count);

        second.Dispose();
        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(3, count);

        first.Dispose();
        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Unsubscribe_ByDelegate_RemovesAllMatching()
    {
        var bus = new EventBus();
        int count = 0;
        Func<OtherTestEvent, CancellationToken, Task> handler = (_, _) =>
        {
            count++;
            return Task.CompletedTask;
        };

        bus.Subscribe(handler);
        bus.Subscribe(handler);

        int removed = bus.Unsubscribe(handler);
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, removed);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Unsubscribe_ByInstance_RemovesAllMatching()
    {
        var bus = new EventBus();
        var handler = new CountingOtherHandler();

        bus.Subscribe<OtherTestEvent>(handler);
        bus.Subscribe<OtherTestEvent>(handler);

        int removed = bus.Unsubscribe<OtherTestEvent>(handler);
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, removed);
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public async Task Unsubscribe_ByType_RemovesAllMatching()
    {
        var bus = new EventBus();
        StaticCountingHandler.Reset();

        bus.Subscribe<OtherTestEvent, StaticCountingHandler>();
        int removed = bus.Unsubscribe<OtherTestEvent, StaticCountingHandler>();
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, removed);
        Assert.Equal(0, StaticCountingHandler.Count);
    }

    [Fact]
    public async Task ClearTEvent_OnlyAffectsExactKey()
    {
        var bus = new EventBus();
        int baseCount = 0;
        int derivedCount = 0;

        bus.Subscribe<BaseTestEvent>((_, _) => { baseCount++; return Task.CompletedTask; });
        bus.Subscribe<DerivedTestEvent>((_, _) => { derivedCount++; return Task.CompletedTask; });

        int removed = bus.Clear<DerivedTestEvent>();
        await bus.PublishAsync(new DerivedTestEvent());

        Assert.Equal(1, removed);
        Assert.Equal(1, baseCount);
        Assert.Equal(0, derivedCount);
    }

    [Fact]
    public async Task Clear_RemovesAll()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<OtherTestEvent>((_, _) => { count++; return Task.CompletedTask; });
        bus.Subscribe<BaseTestEvent>((_, _) => { count++; return Task.CompletedTask; });

        bus.Clear();
        await bus.PublishAsync(new DerivedTestEvent());
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReentrantPublish_Works()
    {
        var bus = new EventBus();
        int innerCount = 0;

        bus.Subscribe<OtherTestEvent>((_, _) => bus.PublishAsync(new InnerTestEvent()));
        bus.Subscribe<InnerTestEvent>((_, _) => { innerCount++; return Task.CompletedTask; });

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, innerCount);
    }

    [Fact]
    public async Task SubscribeDuringPublish_NotInvokedUntilNextPublish()
    {
        var bus = new EventBus();
        int lateCount = 0;
        bool subscribed = false;

        bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            if (!subscribed)
            {
                subscribed = true;
                bus.Subscribe<OtherTestEvent>((_, _) => { lateCount++; return Task.CompletedTask; });
            }

            return Task.CompletedTask;
        });

        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(0, lateCount);

        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(1, lateCount);
    }

    [Fact]
    public async Task TokenDisposedDuringPublish_CurrentSnapshotStillInvokes()
    {
        var bus = new EventBus();
        int count = 0;
        IDisposable? token = null;
        token = bus.Subscribe<OtherTestEvent>((_, _) =>
        {
            token!.Dispose();
            count++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(1, count);

        await bus.PublishAsync(new OtherTestEvent());
        Assert.Equal(1, count);
    }
}

/// <summary>实例订阅用计数器。</summary>
public sealed class CountingOtherHandler : IEventHandler<OtherTestEvent>
{
    public int Count;

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        Count++;
        return Task.CompletedTask;
    }
}

/// <summary>类型订阅用计数器；测试内串行使用。</summary>
public sealed class StaticCountingHandler : IEventHandler<OtherTestEvent>
{
    private static int _count;

    public static int Count => Volatile.Read(ref _count);

    public static void Reset() => Volatile.Write(ref _count, 0);

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return Task.CompletedTask;
    }
}
