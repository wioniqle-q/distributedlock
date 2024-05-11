namespace DistributedLock.Interfaces;

public interface ITimestampManager
{
    public void AssignTimestamp(long transactionId);
    public void RemoveTimestamp(long transactionId);
}