namespace DistributedLock.Interfaces;

public interface IDistributedTwoPhaseCommit
{
    public void AddTransaction(long transactionId);
    public bool Commit(long transactionId, IEnumerable<int> participantIds);
}