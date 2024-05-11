using DistributedLock.Interfaces;

namespace DistributedLock.Abstracts;

public abstract class DistributedTwoPhaseCommitAbstract : IDistributedTwoPhaseCommit
{
    public abstract void AddTransaction(long transactionId);
    public abstract bool Commit(long transactionId, IEnumerable<int> participantIds);
}