using DistributedLock.Enums;

namespace DistributedLock.Interfaces;

internal interface ILockRequest
{
    public long TransactionId { get; set; }
    public LockMode LockMode { get; }
    public DateTime EnqueuedTime { get; }
}