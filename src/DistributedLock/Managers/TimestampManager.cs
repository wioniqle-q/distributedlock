using System.Collections.Concurrent;
using DistributedLock.Abstracts;

namespace DistributedLock.Managers;

public sealed class TimestampManager : TimestampManagerAbstract
{
    private readonly ConcurrentDictionary<long, long> _transactionTimestamps = new();
    private long _globalTimestamp;

    private long GetNextTimestamp()
    {
        return Interlocked.Increment(ref _globalTimestamp);
    }

    public override void AssignTimestamp(long transactionId)
    {
        var timestamp = GetNextTimestamp();
        _transactionTimestamps[transactionId] = timestamp;
        Console.WriteLine($"Transaction {transactionId} assigned timestamp {timestamp}");
    }

    public override void RemoveTimestamp(long transactionId)
    {
        _transactionTimestamps.TryRemove(transactionId, out _);
    }
}