using DistributedLock.Enums;

namespace DistributedLock.Interfaces;

public interface ITransactionEntry
{
    public IEnumerable<KeyValuePair<int, LockMode>> Locks { get; }
    public void AddLock(int resourceId, LockMode lockMode);
    public void RemoveLock(int resourceId);
}