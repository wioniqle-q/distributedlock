using DistributedLock.Enums;

namespace DistributedLock.Interfaces;

internal interface ILockEntry
{
    public bool CanGrantLock(LockMode lockMode);
    public void GrantLock(LockMode lockMode);
    public void ReleaseLock(LockMode lockMode);
    public bool EnqueueLockRequest(long transactionId, LockMode lockMode);
    public void AbortWaitingRequests();
    public bool WaitingQueueTimeout(TimeSpan timeout);
}