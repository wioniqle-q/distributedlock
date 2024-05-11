using System.Collections.Concurrent;
using DistributedLock.Abstracts;
using DistributedLock.Entries;
using DistributedLock.Enums;
using DistributedLock.Interfaces;

namespace DistributedLock.Managers;

public sealed class DistributedLockManager : DistributedLockManagerAbstract
{
    private readonly List<int> _deadlockDetectionList = [];
    private readonly TimeSpan _deadlockDetectionTimeout = TimeSpan.FromMilliseconds(100);

    private readonly ConcurrentDictionary<int, ILockEntry> _lockTable = new();
    private readonly SemaphoreSlim _lockTableSemaphore = new(1, 3_000_0000);
    private readonly ConcurrentDictionary<long, ITransactionEntry> _transactionTable = new();

    public override bool AcquireLock(long transactionId, int resourceId, LockMode lockMode)
    {
        _lockTableSemaphore.Wait();
        try
        {
            if (_lockTable.TryGetValue(resourceId, out var lockEntry) is false)
            {
                lockEntry = new LockEntry();
                _lockTable.TryAdd(resourceId, lockEntry);
            }

            if (lockEntry.CanGrantLock(lockMode))
            {
                lockEntry.GrantLock(lockMode);
                RegisterTransactionLock(transactionId, resourceId, lockMode);
                return true;
            }

            if (lockEntry.EnqueueLockRequest(transactionId, lockMode) is false) return false;
            _deadlockDetectionList.Add(resourceId);

            return false;
        }
        finally
        {
            _lockTableSemaphore.Release();
        }
    }

    public override void ReleaseLock(long transactionId, int resourceId, LockMode lockMode)
    {
        _lockTableSemaphore.Wait();
        try
        {
            if (_lockTable.TryGetValue(resourceId, out var lockEntry) is false) return;

            lockEntry.ReleaseLock(lockMode);
            UnregisterTransactionLock(transactionId, resourceId);
        }
        finally
        {
            _lockTableSemaphore.Release();
        }
    }

    public override void DetectDeadlocks()
    {
        foreach (var resourceId in _deadlockDetectionList)
        {
            if (_lockTable.TryGetValue(resourceId, out var lockEntry) is false) continue;
            if (lockEntry.WaitingQueueTimeout(_deadlockDetectionTimeout) is false) continue;

            lockEntry.AbortWaitingRequests();
            Console.WriteLine($"Deadlock detected on resource {resourceId}.");
        }

        Console.WriteLine("Deadlock detection completed.");
        _deadlockDetectionList.Clear();
    }

    private void RegisterTransactionLock(long transactionId, int resourceId, LockMode lockMode)
    {
        if (_transactionTable.TryGetValue(transactionId, out var transactionEntry) is false)
        {
            transactionEntry = new TransactionEntry();
            _transactionTable.TryAdd(transactionId, transactionEntry);
        }

        transactionEntry.AddLock(resourceId, lockMode);
    }

    private void UnregisterTransactionLock(long transactionId, int resourceId)
    {
        if (_transactionTable.TryGetValue(transactionId, out var transactionEntry))
            transactionEntry.RemoveLock(resourceId);
    }

    private sealed class LockEntry : ILockEntry
    {
        private static readonly bool[,] CompatibilityMatrix =
        {
            //    NL   IS   IX    S    SIX   X
            { true, true, true, true, true, false }, // NL
            { true, true, false, true, false, false }, // IS
            { true, false, false, false, false, false }, // IX
            { true, true, false, true, false, false }, // S
            { true, false, false, false, false, false }, // SIX
            { false, false, false, false, false, false } // X
        };

        private readonly ConcurrentDictionary<LockMode, int> _grantedLocks = new();
        private readonly Queue<ILockRequest> _waitingQueue = new();

        public bool CanGrantLock(LockMode lockMode)
        {
            return _grantedLocks.All(grantedLock => CompatibilityMatrix[(int)grantedLock.Key, (int)lockMode]);
        }

        public void GrantLock(LockMode lockMode)
        {
            if (_grantedLocks.TryAdd(lockMode, 1) is false) _grantedLocks[lockMode]++;
        }

        public void ReleaseLock(LockMode lockMode)
        {
            if (_grantedLocks.TryGetValue(lockMode, out var value))
            {
                _grantedLocks[lockMode] = --value;

                if (value is 0) _grantedLocks.TryRemove(lockMode, out _);
            }

            if (_waitingQueue.Count <= 0 || CanGrantLock(_waitingQueue.Peek().LockMode) is false) return;

            var request = _waitingQueue.Dequeue();
            GrantLock(request.LockMode);
        }

        public bool EnqueueLockRequest(long transactionId, LockMode lockMode)
        {
            var request = new LockRequest(transactionId, lockMode, DateTime.UtcNow);
            _waitingQueue.Enqueue(request);

            return true;
        }

        public void AbortWaitingRequests()
        {
            _waitingQueue.Clear();
        }

        public bool WaitingQueueTimeout(TimeSpan timeout)
        {
            if (_waitingQueue.Count is 0) return false;

            return DateTime.UtcNow - _waitingQueue.Peek().EnqueuedTime > timeout;
        }

        private class LockRequest(long transactionId, LockMode lockMode, DateTime enqueuedTime) : ILockRequest
        {
            public long TransactionId { get; set; } = transactionId;
            public LockMode LockMode { get; } = lockMode;
            public DateTime EnqueuedTime { get; } = enqueuedTime;
        }
    }
}