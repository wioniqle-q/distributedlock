using DistributedLock.Interfaces;

namespace DistributedLock.Abstracts;

public abstract class DeadlockDetectionAbstract : IDeadlockDetection
{
    public abstract ValueTask DisposeAsync();
}