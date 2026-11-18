using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

public sealed class EventBusDiTests
{
    [Fact]
    public async Task ScopedHandler_ResolvedInOwnScope()
    {
        var counter = new Counter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddScoped<ScopedCountingHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider);
        bus.Subscribe<OtherTestEvent, ScopedCountingHandler>();

        await bus.PublishAsync(new OtherTestEvent());
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task TwoScopedHandlers_GetDifferentScopes()
    {
        var probe = new ScopeProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddScoped<ScopedDependency>();
        services.AddScoped<FirstScopedHandler>();
        services.AddScoped<SecondScopedHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider);
        bus.Subscribe<OtherTestEvent, FirstScopedHandler>();
        bus.Subscribe<OtherTestEvent, SecondScopedHandler>();

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, probe.Ids.Distinct().Count());
    }

    [Fact]
    public async Task SingletonHandler_IsShared()
    {
        var handler = new SingletonCountingHandler();
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider);
        bus.Subscribe<OtherTestEvent, SingletonCountingHandler>();

        await bus.PublishAsync(new OtherTestEvent());
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, handler.Count);
    }

    [Fact]
    public async Task TransientHandler_ResolvedPerPublish()
    {
        TransientCountingHandler.Reset();
        var services = new ServiceCollection();
        services.AddTransient<TransientCountingHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider);
        bus.Subscribe<OtherTestEvent, TransientCountingHandler>();

        await bus.PublishAsync(new OtherTestEvent());
        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(2, TransientCountingHandler.Constructed);
    }

    [Fact]
    public async Task InterfaceRegistration_Resolved()
    {
        var counter = new Counter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddScoped<IEventHandler<OtherTestEvent>, InterfaceRegisteredHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var bus = new EventBus(provider);
        bus.Subscribe<OtherTestEvent, InterfaceRegisteredHandler>();

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ActivatorFallback_Works()
    {
        ParameterlessCountingHandler.Reset();
        var bus = new EventBus();
        bus.Subscribe<OtherTestEvent, ParameterlessCountingHandler>();

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, ParameterlessCountingHandler.Count);
    }

    [Fact]
    public async Task ConstructorThrows_RecordsError()
    {
        var bus = new EventBus(new EventBusOptions { ThrowOnError = false });
        bus.Subscribe<OtherTestEvent, ThrowingConstructorHandler>();

        EventDispatchResult result = await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors[0].Exception);
    }

    [Fact]
    public async Task DisposableActivatorHandler_IsDisposed()
    {
        DisposableActivatorHandler.Reset();
        var bus = new EventBus();
        bus.Subscribe<OtherTestEvent, DisposableActivatorHandler>();

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, DisposableActivatorHandler.DisposeCount);
    }

    [Fact]
    public async Task AsyncDisposableActivatorHandler_IsDisposed()
    {
        AsyncDisposableActivatorHandler.Reset();
        var bus = new EventBus();
        bus.Subscribe<OtherTestEvent, AsyncDisposableActivatorHandler>();

        await bus.PublishAsync(new OtherTestEvent());

        Assert.Equal(1, AsyncDisposableActivatorHandler.DisposeCount);
    }
}

public sealed class Counter
{
    public int Count;
}

public sealed class ScopeProbe
{
    public ConcurrentBag<Guid> Ids { get; } = new();
}

public sealed class ScopedDependency
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ScopedCountingHandler : IEventHandler<OtherTestEvent>
{
    private readonly Counter _counter;

    public ScopedCountingHandler(Counter counter) => _counter = counter;

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _counter.Count);
        return Task.CompletedTask;
    }
}

public sealed class FirstScopedHandler : IEventHandler<OtherTestEvent>
{
    private readonly ScopedDependency _dependency;
    private readonly ScopeProbe _probe;

    public FirstScopedHandler(ScopedDependency dependency, ScopeProbe probe)
    {
        _dependency = dependency;
        _probe = probe;
    }

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        _probe.Ids.Add(_dependency.Id);
        return Task.CompletedTask;
    }
}

public sealed class SecondScopedHandler : IEventHandler<OtherTestEvent>
{
    private readonly ScopedDependency _dependency;
    private readonly ScopeProbe _probe;

    public SecondScopedHandler(ScopedDependency dependency, ScopeProbe probe)
    {
        _dependency = dependency;
        _probe = probe;
    }

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        _probe.Ids.Add(_dependency.Id);
        return Task.CompletedTask;
    }
}

public sealed class SingletonCountingHandler : IEventHandler<OtherTestEvent>
{
    public int Count;

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        Count++;
        return Task.CompletedTask;
    }
}

public sealed class TransientCountingHandler : IEventHandler<OtherTestEvent>
{
    private static int _constructed;

    public static int Constructed => Volatile.Read(ref _constructed);

    public static void Reset() => Volatile.Write(ref _constructed, 0);

    public TransientCountingHandler()
        => Interlocked.Increment(ref _constructed);

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class InterfaceRegisteredHandler : IEventHandler<OtherTestEvent>
{
    private readonly Counter _counter;

    public InterfaceRegisteredHandler(Counter counter) => _counter = counter;

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _counter.Count);
        return Task.CompletedTask;
    }
}

public sealed class ParameterlessCountingHandler : IEventHandler<OtherTestEvent>
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

public sealed class DisposableActivatorHandler : IEventHandler<OtherTestEvent>, IDisposable
{
    private static int _disposeCount;

    public static int DisposeCount => Volatile.Read(ref _disposeCount);

    public static void Reset() => Volatile.Write(ref _disposeCount, 0);

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}

public sealed class AsyncDisposableActivatorHandler : IEventHandler<OtherTestEvent>, IAsyncDisposable
{
    private static int _disposeCount;

    public static int DisposeCount => Volatile.Read(ref _disposeCount);

    public static void Reset() => Volatile.Write(ref _disposeCount, 0);

    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeCount);
        return ValueTask.CompletedTask;
    }
}
