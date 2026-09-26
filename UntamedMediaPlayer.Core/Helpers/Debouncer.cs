using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

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
    private readonly ILogger<Debouncer> _logger = Ioc.Default.GetRequiredService<ILogger<Debouncer>>();
    private readonly Lock _gate = new();
    private readonly Action<Exception>? _onError;
    private readonly Timer _timer; // Reuse the same Timer

    private PendingCall? _pending; // The pending call (at most one)
    private CancellationTokenSource? _executingCts; // Cancellation source for the executing call
    private long _cancelEpoch; // Incremented by Cancel/Dispose for race detection
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
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay cannot be negative.");
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
            catch (ObjectDisposedException) { } // Races with ExecuteAsync's finally block
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
                // Ignore
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
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        }

        // Avoid allocating a TCS for fire-and-forget calls
        TaskCompletionSource<bool>? tcs = wantResult
            ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;

        PendingCall? previous;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(Debouncer));

            previous = _pending;
            _pending = new PendingCall<TState>(action, state, tcs);

            // Reuse the same Timer and atomically reset its due time
            _timer.Change(effectiveDelay, Timeout.InfiniteTimeSpan);
        }

        // Complete the replaced call immediately so callers do not wait unnecessarily
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
            // Cancel / Dispose may occur between taking the pending call and starting execution
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

            // Cancel / Dispose may have already released it; disposing it again is safe
            cts.Dispose();
        }
    }

    private void HandleError(Exception ex)
    {
        if (_onError is null)
        {
            // Do not allow exceptions to escape to the thread pool
            _logger.ZLogError(ex, $"[Debouncer] Unhandled callback exception");
            return;
        }

        try
        {
            _onError(ex);
        }
        catch (Exception he)
        {
            _logger.ZLogError(he, $"[Debouncer] Error handler threw an exception");
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
