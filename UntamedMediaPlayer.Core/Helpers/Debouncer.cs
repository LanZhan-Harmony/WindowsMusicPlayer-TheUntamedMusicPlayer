namespace UntamedMediaPlayer.Core.Helpers;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 防抖器
/// <para>
/// 连续多次调用时，只有「静默 <see cref="DefaultDelay"/> 后仍未被取代」的
/// 最后一次调用会被执行；期间每次调用都会重置计时器。
/// </para>
/// </summary>
public sealed class Debouncer : IDisposable
{
    private readonly Lock _gate = new();
    private readonly TimeSpan _defaultDelay;
    private readonly Action<Exception>? _onError;
    private readonly Timer _timer; // 复用同一个 Timer

    private PendingCall? _pending; // 等待中的调用（最多一个）
    private CancellationTokenSource? _executingCts; // 正在执行的调用的取消源
    private long _cancelEpoch; // Cancel/Dispose 时递增，用于竞态检测
    private bool _disposed;

    public Debouncer(TimeSpan delay, Action<Exception>? onError = null)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "延迟不能为负数。");
        }

        _defaultDelay = delay;
        _onError = onError;
        _timer = new Timer(
            static s => ((Debouncer)s!).OnTimerElapsed(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan
        );
    }

    public Debouncer(int millisecondsDelay, Action<Exception>? onError = null)
        : this(TimeSpan.FromMilliseconds(millisecondsDelay), onError) { }

    /// <summary>默认静默时长。</summary>
    public TimeSpan DefaultDelay => _defaultDelay;

    /// <summary>是否存在尚未执行的挂起调用。</summary>
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

    /// <summary>是否已释放。</summary>
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

    // ================== Fire-and-forget 重载 ==================

    /// <summary>提交一次调用，不关心是否真正执行。</summary>
    public void Debounce(Action action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = ScheduleAsync(
            static (_, s) =>
            {
                ((Action)s!).Invoke();
                return Task.CompletedTask;
            },
            action,
            delay,
            wantResult: false
        );
    }

    /// <summary>提交一次异步调用，不关心是否真正执行。</summary>
    public void Debounce(Func<Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = ScheduleAsync(
            static (_, s) => ((Func<Task>)s!).Invoke(),
            action,
            delay,
            wantResult: false
        );
    }

    // ================== Await 重载 ==================

    /// <summary>
    /// 提交一次调用并等待结果。
    /// 返回 <c>true</c> 表示本次调用真正执行；<c>false</c> 表示被后续调用取代。
    /// </summary>
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
            wantResult: true
        );
    }

    /// <summary>提交一次异步调用并等待结果。</summary>
    public Task<bool> RunAsync(Func<Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ScheduleAsync(
            static (_, s) => ((Func<Task>)s!).Invoke(),
            action,
            delay,
            wantResult: true
        );
    }

    /// <summary>提交一次带取消令牌的异步调用并等待结果。</summary>
    public Task<bool> RunAsync(Func<CancellationToken, Task> action, TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ScheduleAsync(
            static (ct, s) => ((Func<CancellationToken, Task>)s!).Invoke(ct),
            action,
            delay,
            wantResult: true
        );
    }

    // ================== 控制方法 ==================

    /// <summary>
    /// 取消挂起调用，并取消正在执行的调用（若其响应取消令牌）。
    /// 已经完成、且不响应取消令牌的同步调用不受影响。
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
    /// 立即执行挂起的调用（如果有），并重置计时器。
    /// 适合窗口关闭、页面离开等需要立即落盘的场景。
    /// 若当前没有挂起项，则什么也不做。
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
    /// 释放资源、弃置挂起调用、取消正在执行的任务。可重复调用。
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
            catch { } // ignore
        }

        GC.SuppressFinalize(this);
    }

    // ================== 内部实现 ==================

    private Task<bool> ScheduleAsync<TState>(
        Func<CancellationToken, TState, Task> action,
        TState state,
        TimeSpan? delay,
        bool wantResult
    )
    {
        TimeSpan effectiveDelay = delay ?? _defaultDelay;
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

        return (Task<bool>)(tcs?.Task ?? Task.CompletedTask);
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

    private async Task ExecuteAsync(PendingCall call) =>
        await ExecuteAsync(call, -1).ConfigureAwait(false);

    private async Task ExecuteAsync(PendingCall call, long epochAtSchedule)
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
        private readonly Func<CancellationToken, TState, Task> _action = action;
        private readonly TState _state = state;

        public override Task InvokeAsync(CancellationToken ct) => _action(ct, _state);
    }
}
