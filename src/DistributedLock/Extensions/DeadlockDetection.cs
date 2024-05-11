using DistributedLock.Abstracts;
using DistributedLock.Interfaces;

namespace DistributedLock.Extensions;

public sealed class DeadlockDetection : DeadlockDetectionAbstract, IAsyncDisposable
{
    private readonly IDistributedLockManager _lockManager;
    private readonly Timer _timer;
    private bool _disposed;

    public DeadlockDetection(IDistributedLockManager lockManager, double interval)
    {
        _lockManager = lockManager;
        _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        _timer.Change(0, (long)interval);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;
        await _timer.DisposeAsync();
    }

    private void OnTimerElapsed(object? state)
    {
        _lockManager.DetectDeadlocks();
    }
}