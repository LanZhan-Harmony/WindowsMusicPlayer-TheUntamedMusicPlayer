using System.Diagnostics;

namespace UntamedMediaPlayer.Core.Helpers;

/// <summary>
/// Debouncer
/// </summary>
/// <remarks>
/// When multiple calls are made in quick succession,
/// only the last call that remains un-replaced after a silent period of <see cref="DefaultDelay" /> will be executed;
/// each call during this period resets the timer.
/// </remarks>
public sealed class Debouncer : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Action<Exception>? _onError;
    private readonly Timer _timer; // 复用同一个 Timer

    private PendingCall? _pending; // 等待中的调用（最多一个）
    private CancellationTokenSource? _executingCts; // 正在执行的调用的取消源
    private long _cancelEpoch; // Cancel/Dispose 时递增，用于竞态检测
    private bool _disposed;

    /// <summary>Default silent period</summary>
    public TimeSpan DefaultDelay { get; }

    /// <summary>If there are pending calls that have not been executed yet</summary>
    public bool IsPending
    {
        get
        {
            lock (_gate)
            {
                return _pending is not null;
            }
        }
    }

    /// <summary>If the debouncer has been disposed.</summary>
    public bool IsDisposed
    {
        get
        {
            lock (_gate)
            {
                return _disposed;
            }
        }
    }

    public Debouncer(TimeSpan delay, Action<Exception>? onError = null)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "延迟不能为负数。");
        }

        DefaultDelay = delay;
        _onError = onError;
        _timer = new Timer(
            static s => ((Debouncer)s!).OnTimerElapsed(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan
        );
    }

    // ================== Fire-and-forget Overloads ==================

    /// <summary>Submit a call without caring if it actually executes</summary>
    public void Debounce(Action action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = ScheduleAsync(
            static (_, s) =>
            {
                s.Invoke();
                return Task.CompletedTask;
            },
            action,
            delay,
            false
        );
    }

    /// <summary>Submit an asynchronous call without caring if it actually executes</summary>
    public void Debounce(Func<Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = ScheduleAsync(
            static (_, s) => s.Invoke(),
            action,
            delay,
            false
        );
    }

    // ================== Await Overloads ==================

    /// <summary>
    /// Submit a call and wait for the result
    /// </summary>
    /// <param name="action">The action to submit</param>
    /// <param name="delay">The delay before executing the action</param>
    /// <returns><c>true</c> if the action was executed, <c>false</c> if it was cancelled</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<bool> RunAsync(Action action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ScheduleAsync(
            static (_, s) =>
            {
                s.Invoke();
                return Task.CompletedTask;
            },
            action,
            delay,
            true
        );
    }

    /// <summary>Submit an asynchronous call and wait for the result</summary>
    /// <param name="action">The asynchronous action to submit</param>
    /// <param name="delay">The delay before executing the action</param>
    /// <returns><c>true</c> if the action was executed, <c>false</c> if it was cancelled</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<bool> RunAsync(Func<Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ScheduleAsync(
            static (_, s) => s.Invoke(),
            action,
            delay,
            true
        );
    }
    /// <summary>
    /// Submit an asynchronous call with cancellation support and wait for the result
    /// </summary>
    /// <param name="action">The asynchronous action to submit</param>
    /// <param name="delay">The delay before executing the action</param>
    /// <returns><c>true</c> if the action was executed, <c>false</c> if it was cancelled</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<bool> RunAsync(Func<CancellationToken, Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ScheduleAsync(
            static (ct, s) => s.Invoke(ct),
            action,
            delay,
            true
        );
    }

    /// <summary>
    /// Cancel the pending call and cancel the currently executing call (if it responds to the cancellation token).
    /// Already completed synchronous calls that do not respond to the cancellation token are not affected.
    /// </summary>
    public void Cancel()
    {
        PendingCall? pending;
        CancellationTokenSource? execCts;

        lock (_gate)
        {
            _cancelEpoch++;
            pending = _pending;
            _pending = null;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            execCts = _executingCts;
        }

        pending?.Completion?.TrySetResult(false);

        if (execCts is not null)
        {
            try
            {
                execCts.Cancel();
            }
            catch (ObjectDisposedException) { } // 与 ExecuteAsync 的 finally 竞争
        }
    }

    /// <summary>
    /// Execute the pending call immediately (if any) and reset the timer.
    /// Suitable for scenarios where you need to persist data immediately,
    /// such as when closing a window or leaving a page.
    /// If there is no pending call, do nothing.
    /// </summary>
    public void Flush()
    {
        PendingCall? pending;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(Debouncer));
            pending = _pending;
            _pending = null;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        if (pending is not null)
        {
            _ = ExecuteAsync(pending);
        }
    }

    /// <summary>
    /// Release resources, discard pending calls, and cancel executing tasks.
    /// Can be called multiple times.
    /// </summary>
    public void Dispose()
    {
        PendingCall? pending;
        CancellationTokenSource? execCts;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _cancelEpoch++;
            pending = _pending;
            _pending = null;

            _timer.Dispose();

            execCts = _executingCts;
            _executingCts = null;
        }

        pending?.Completion?.TrySetResult(false);

        if (execCts is not null)
        {
            try
            {
                execCts.Cancel();
            }
            catch
            {
                // ignore
            }
        }

        GC.SuppressFinalize(this);
    }

    private Task<bool> ScheduleAsync<TState>(
        Func<CancellationToken, TState, Task> action,
        TState state,
        TimeSpan? delay,
        bool wantResult
    )
    {
        TimeSpan effectiveDelay = delay ?? DefaultDelay;
        if (effectiveDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "延迟不能为负数。");
        }

        // fire-and-forget 时不分配 TCS
        TaskCompletionSource<bool>? tcs = wantResult
            ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;

        PendingCall? previous;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(Debouncer));

            previous = _pending;
            _pending = new PendingCall<TState>(action, state, tcs);

            // 复用同一个 Timer，仅原子地重置到期时间
            _timer.Change(effectiveDelay, Timeout.InfiniteTimeSpan);
        }

        // 被取代的调用立刻完成，避免调用方无谓等待
        previous?.Completion?.TrySetResult(false);

        return tcs?.Task ?? Task.FromResult(false);
    }

    private void OnTimerElapsed()
    {
        PendingCall? call;
        long epoch;

        lock (_gate)
        {
            call = _pending;
            _pending = null;
            epoch = _cancelEpoch;
        }

        if (call is not null)
        {
            _ = ExecuteAsync(call, epoch);
        }
    }

    private async Task ExecuteAsync(PendingCall call)
    {
        await ExecuteAsync(call, -1).ConfigureAwait(false);
    }

    private async ValueTask ExecuteAsync(PendingCall call, long epochAtSchedule)
    {
        CancellationTokenSource cts;

        lock (_gate)
        {
            // Cancel / Dispose 恰好发生在「取走挂起项」与「开始执行」之间
            if (_disposed || (epochAtSchedule >= 0 && _cancelEpoch != epochAtSchedule))
            {
                call.Completion?.TrySetResult(false);
                return;
            }

            cts = new CancellationTokenSource();
            _executingCts = cts;
        }

        try
        {
            await call.InvokeAsync(cts.Token).ConfigureAwait(false);
            call.Completion?.TrySetResult(true);
        }
        catch (OperationCanceledException)
        {
            call.Completion?.TrySetResult(false);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            call.Completion?.TrySetException(ex);
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_executingCts, cts))
                {
                    _executingCts = null;
                }
            }

            // Cancel / Dispose 可能已经释放了它；再次 Dispose 是安全的
            cts.Dispose();
        }
    }

    private void HandleError(Exception ex)
    {
        if (_onError is null)
        {
            // 不允许异常逃逸到线程池
            Debug.WriteLine($"[Debouncer] 回调抛出未处理异常: {ex}");
            return;
        }

        try
        {
            _onError(ex);
        }
        catch (Exception he)
        {
            Debug.WriteLine($"[Debouncer] 错误处理器自身抛出异常: {he}");
        }
    }

    private abstract class PendingCall(TaskCompletionSource<bool>? completion)
    {
        public TaskCompletionSource<bool>? Completion { get; } = completion;

        public abstract Task InvokeAsync(CancellationToken ct);
    }

    private sealed class PendingCall<TState>(
        Func<CancellationToken, TState, Task> action,
        TState state,
        TaskCompletionSource<bool>? completion
    ) : PendingCall(completion)
    {
        public override Task InvokeAsync(CancellationToken ct)
        {
            return action(ct, state);
        }
    }
}
