using DistributedLock.Enums;
using DistributedLock.Interfaces;

namespace DistributedLock.Abstracts;

public abstract class DistributedLockManagerAbstract : IDistributedLockManager
{
    public abstract bool AcquireLock(long transactionId, int resourceId, LockMode lockMode);
    public abstract void ReleaseLock(long transactionId, int resourceId, LockMode lockMode);
    public abstract void DetectDeadlocks();
}