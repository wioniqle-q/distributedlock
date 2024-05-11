using DistributedLock.Enums;

namespace DistributedLock.Interfaces;

public interface IDistributedLockManager
{
    public bool AcquireLock(long transactionId, int resourceId, LockMode lockMode);
    public void ReleaseLock(long transactionId, int resourceId, LockMode lockMode);
    public void DetectDeadlocks();
}