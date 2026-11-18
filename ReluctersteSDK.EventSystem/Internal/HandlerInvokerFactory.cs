using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Internal
{
    /// <summary>包装一次处理器调用，负责 DI Scope、超时、取消与实例释放。</summary>
    internal sealed class HandlerInvokerFactory
    {
        private readonly IServiceProvider? _services;
        private readonly EventBusOptions _options;

        public HandlerInvokerFactory(IServiceProvider? services, EventBusOptions options)
        {
            _services = services;
            _options = options;
        }

        public Func<object, CancellationToken, Task<HandlerOutcome>> CreateTypeInvoker<TEvent, THandler>(string handlerName)
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            return (eventObject, cancellationToken) => InvokeWithLifetimeAsync(
                (provider, token) => CreateTypeCall<TEvent, THandler>(eventObject, provider, token),
                handlerName,
                cancellationToken,
                useServiceScope: true);
        }

        public Func<object, CancellationToken, Task<HandlerOutcome>> CreateInstanceInvoker<TEvent>(
            IEventHandler<TEvent> handler,
            string handlerName)
            where TEvent : IEvent
        {
            return (eventObject, cancellationToken) => InvokeWithLifetimeAsync(
                (_, token) => new HandlerCallResult(handler.HandleAsync((TEvent)eventObject, token), null),
                handlerName,
                cancellationToken,
                useServiceScope: false);
        }

        public Func<object, CancellationToken, Task<HandlerOutcome>> CreateDelegateInvoker<TEvent>(
            Func<TEvent, CancellationToken, Task> handler,
            string handlerName)
            where TEvent : IEvent
        {
            return (eventObject, cancellationToken) => InvokeWithLifetimeAsync(
                (_, token) => new HandlerCallResult(handler((TEvent)eventObject, token), null),
                handlerName,
                cancellationToken,
                useServiceScope: false);
        }

        public Func<object, CancellationToken, Task<HandlerOutcome>> CreateMethodInvoker(
            object target,
            MethodInfo method,
            string handlerName)
        {
            return (eventObject, cancellationToken) => InvokeWithLifetimeAsync(
                (_, token) => new HandlerCallResult(InvokeMethod(target, method, eventObject, token), null),
                handlerName,
                cancellationToken,
                useServiceScope: false);
        }

        private HandlerCallResult CreateTypeCall<TEvent, THandler>(
            object eventObject,
            IServiceProvider? provider,
            CancellationToken cancellationToken)
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>
        {
            (object instance, object? extra) = ResolveHandler(provider, typeof(THandler), typeof(IEventHandler<TEvent>));

            if (instance is not IEventHandler<TEvent> typed)
            {
                DisposeExtraBestEffort(extra);
                throw new InvalidOperationException(
                    $"处理器 {typeof(THandler).FullName} 未实现 IEventHandler<{typeof(TEvent).Name}>。");
            }

            try
            {
                Task task = typed.HandleAsync((TEvent)eventObject, cancellationToken);
                return new HandlerCallResult(task, extra);
            }
            catch
            {
                DisposeExtraBestEffort(extra);
                throw;
            }
        }

        private (object Instance, object? ExtraDisposable) ResolveHandler(
            IServiceProvider? provider,
            Type handlerType,
            Type handlerInterfaceType)
        {
            if (provider is not null)
            {
                object? instance = provider.GetService(handlerType);
                if (instance is null)
                {
                    foreach (object? candidate in provider.GetServices(handlerInterfaceType))
                    {
                        if (candidate is not null && handlerType.IsInstanceOfType(candidate))
                        {
                            instance = candidate;
                            break;
                        }
                    }
                }

                if (instance is not null)
                    return (instance, null);
            }

            if (!_options.EnableActivatorFallback)
            {
                throw new InvalidOperationException(
                    $"无法解析处理器 {handlerType.FullName}：未在 DI 容器中注册，且 EnableActivatorFallback=false。");
            }

            object created = Activator.CreateInstance(handlerType)
                ?? throw new InvalidOperationException($"无法创建处理器 {handlerType.FullName}。");

            object? extra = created is IDisposable or IAsyncDisposable ? created : null;
            return (created, extra);
        }

        private async Task<HandlerOutcome> InvokeWithLifetimeAsync(
            Func<IServiceProvider?, CancellationToken, HandlerCallResult> handlerCall,
            string handlerName,
            CancellationToken publishToken,
            bool useServiceScope)
        {
            AsyncServiceScope scope = default;
            bool hasScope = false;
            CancellationTokenSource? timeoutCts = null;
            CancellationTokenSource? linkedCts = null;
            CancellationToken handlerToken = publishToken;

            try
            {
                if (useServiceScope && _services is not null)
                {
                    scope = _services.CreateAsyncScope();
                    hasScope = true;
                }

                if (_options.HandlerTimeout is { } timeout)
                {
                    timeoutCts = new CancellationTokenSource(timeout);
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(publishToken, timeoutCts.Token);
                    handlerToken = linkedCts.Token;
                }

                HandlerCallResult call;
                try
                {
                    call = handlerCall(hasScope ? scope.ServiceProvider : null, handlerToken);
                    if (call.Task is null)
                        throw new InvalidOperationException($"处理器 {handlerName} 返回了 null Task。");
                }
                catch (Exception ex)
                {
                    return HandlerOutcome.Error(ex);
                }

                Task handlerTask = call.Task;
                object? extra = call.ExtraDisposable;

                if (timeoutCts is null)
                {
                    try
                    {
                        await handlerTask;
                        return HandlerOutcome.Success;
                    }
                    catch (OperationCanceledException) when (publishToken.IsCancellationRequested)
                    {
                        return HandlerOutcome.Canceled;
                    }
                    catch (Exception ex)
                    {
                        return HandlerOutcome.Error(ex);
                    }
                    finally
                    {
                        await DisposeExtraAsync(extra).ConfigureAwait(false);
                        if (hasScope)
                            await scope.DisposeAsync().ConfigureAwait(false);
                    }
                }

                CancellationTokenSource timeoutSource = timeoutCts;
                _ = DisposeAfterTaskAsync(scope, hasScope, handlerTask, extra, linkedCts, timeoutSource);
                hasScope = false;
                linkedCts = null;
                timeoutCts = null;

                try
                {
                    await handlerTask.WaitAsync(_options.HandlerTimeout!.Value, publishToken);
                    return HandlerOutcome.Success;
                }
                catch (TimeoutException) when (timeoutSource.IsCancellationRequested)
                {
                    return HandlerOutcome.Timeout(
                        new TimeoutException($"处理器 {handlerName} 在 {_options.HandlerTimeout} 内未完成。"));
                }
                catch (OperationCanceledException) when (publishToken.IsCancellationRequested)
                {
                    return HandlerOutcome.Canceled;
                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    return HandlerOutcome.Timeout(
                        new TimeoutException($"处理器 {handlerName} 在 {_options.HandlerTimeout} 内被取消。"));
                }
                catch (Exception ex)
                {
                    return HandlerOutcome.Error(ex);
                }
            }
            finally
            {
                if (hasScope)
                    await scope.DisposeAsync().ConfigureAwait(false);

                linkedCts?.Dispose();
                timeoutCts?.Dispose();
            }
        }

        private static async Task DisposeAfterTaskAsync(
            AsyncServiceScope scope,
            bool hasScope,
            Task task,
            object? extra,
            CancellationTokenSource? linkedCts,
            CancellationTokenSource? timeoutCts)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // 超时后处理器才结束：这里只负责观察迟到异常，不修改已返回的结果。
            }
            finally
            {
                try
                {
                    await DisposeExtraAsync(extra).ConfigureAwait(false);
                }
                catch
                {
                    // 释放失败不能影响发布方。
                }

                linkedCts?.Dispose();
                timeoutCts?.Dispose();

                if (hasScope)
                {
                    try
                    {
                        await scope.DisposeAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // 释放失败不能影响发布方。
                    }
                }
            }
        }

        private static async ValueTask DisposeExtraAsync(object? extra)
        {
            if (extra is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else if (extra is IDisposable disposable)
                disposable.Dispose();
        }

        private static void DisposeExtraBestEffort(object? extra)
        {
            try
            {
                if (extra is IAsyncDisposable asyncDisposable)
                    _ = asyncDisposable.DisposeAsync();
                else if (extra is IDisposable disposable)
                    disposable.Dispose();
            }
            catch
            {
                // 同步路径上的尽力释放，失败不影响原始异常。
            }
        }

        private static Task InvokeMethod(object target, MethodInfo method, object eventObject, CancellationToken cancellationToken)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object?[] arguments = parameters.Length == 2
                ? new object?[] { eventObject, cancellationToken }
                : new object?[] { eventObject };

            object? result;
            try
            {
                result = method.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            return result switch
            {
                null => Task.CompletedTask,
                Task task => task,
                ValueTask valueTask => valueTask.AsTask(),
                _ => throw new InvalidOperationException($"处理器 {method.Name} 返回了不支持的返回类型 {result.GetType().Name}。"),
            };
        }
    }

    /// <summary>处理器调用的一次性结果：任务与需要额外释放的实例。</summary>
    internal sealed class HandlerCallResult
    {
        public HandlerCallResult(Task? task, object? extraDisposable)
        {
            Task = task;
            ExtraDisposable = extraDisposable;
        }

        public Task? Task { get; }
        public object? ExtraDisposable { get; }
    }
}
