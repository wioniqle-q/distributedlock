using DistributedLock.Interfaces;

namespace DistributedLock.Structs;

public readonly struct VoteResult : IVoteResult
{
    public bool Vote { get; }
    public string Message { get; }

    private VoteResult(bool vote, string message)
    {
        Vote = vote;
        Message = message;
    }

    public static VoteResult Yes(string message = "")
    {
        return new VoteResult(true, message);
    }

    public static VoteResult No(string message)
    {
        return new VoteResult(false, message);
    }
}