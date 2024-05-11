using System.Collections.Concurrent;
using DistributedLock.Abstracts;
using DistributedLock.Enums;

namespace DistributedLock.Entries;

public sealed class TransactionEntry : TransactionEntryAbstract
{
    private readonly ConcurrentDictionary<int, LockMode> _locks = new();

    public override IEnumerable<KeyValuePair<int, LockMode>> Locks => _locks;

    public override void AddLock(int resourceId, LockMode lockMode)
    {
        _locks[resourceId] = lockMode;
    }

    public override void RemoveLock(int resourceId)
    {
        _locks.TryRemove(resourceId, out _);
    }
}