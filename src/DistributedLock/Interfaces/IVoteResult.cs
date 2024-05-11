namespace DistributedLock.Interfaces;

public interface IVoteResult
{
    public bool Vote { get; }
    public string Message { get; }
}