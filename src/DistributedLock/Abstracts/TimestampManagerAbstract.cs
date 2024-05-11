using DistributedLock.Interfaces;

namespace DistributedLock.Abstracts;

public abstract class TimestampManagerAbstract : ITimestampManager
{
    public abstract void AssignTimestamp(long transactionId);
    public abstract void RemoveTimestamp(long transactionId);
}