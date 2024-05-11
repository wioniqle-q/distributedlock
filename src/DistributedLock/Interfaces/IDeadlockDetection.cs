namespace DistributedLock.Interfaces;

public interface IDeadlockDetection
{
    public ValueTask DisposeAsync();
}