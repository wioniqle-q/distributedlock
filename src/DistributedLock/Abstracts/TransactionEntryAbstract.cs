using DistributedLock.Enums;
using DistributedLock.Interfaces;

namespace DistributedLock.Abstracts;

public abstract class TransactionEntryAbstract : ITransactionEntry
{
    public abstract IEnumerable<KeyValuePair<int, LockMode>> Locks { get; }
    public abstract void AddLock(int resourceId, LockMode lockMode);
    public abstract void RemoveLock(int resourceId);
}