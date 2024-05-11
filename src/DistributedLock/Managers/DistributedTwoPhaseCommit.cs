using System.Collections.Concurrent;
using DistributedLock.Abstracts;
using DistributedLock.Interfaces;

namespace DistributedLock.Managers;

public sealed class DistributedTwoPhaseCommit : DistributedTwoPhaseCommitAbstract
{
    private readonly CommunicationManager _communicationManager = CommunicationManager.Instance;
    private readonly ConcurrentDictionary<long, ITwoPhaseTransactionEntry> _transactionTable = new();
    private readonly SemaphoreSlim _transactionTableSemaphore = new(1, 3_000_000);

    public override void AddTransaction(long transactionId)
    {
        _transactionTableSemaphore.Wait();
        try
        {
            _transactionTable[transactionId] = new TwoPhaseTransactionEntry();
        }
        finally
        {
            _transactionTableSemaphore.Release();
        }
    }

    public override bool Commit(long transactionId, IEnumerable<int> participantIds)
    {
        _transactionTableSemaphore.Wait();
        try
        {
            if (_transactionTable.TryGetValue(transactionId, out _) is false)
            {
                Console.WriteLine("Invalid transaction id");
                return false;
            }

            var enumerable = participantIds as int[] ?? participantIds.ToArray();
            var totalVotes = enumerable.Length;

            var voteCount = enumerable.Select(participantId => PrepareParticipant(transactionId, participantId))
                .Count(voteResult => voteResult);
            if (voteCount == totalVotes)
            {
                foreach (var participantId in enumerable)
                    CommitParticipant(transactionId, participantId);

                _transactionTable.TryRemove(transactionId, out _);
                return true;
            }

            foreach (var participantId in enumerable)
                RollbackParticipant(transactionId, participantId);

            Console.WriteLine("Transaction is rolled back");
            _transactionTable.TryRemove(transactionId, out _);
            return false;
        }
        finally
        {
            _transactionTableSemaphore.Release();
        }
    }

    private bool PrepareParticipant(long transactionId, int participantId)
    {
        var voteResult = _communicationManager.SendPrepareRequest(participantId, transactionId);
        return voteResult.Vote;
    }

    private void CommitParticipant(long transactionId, int participantId)
    {
        _communicationManager.SendCommitRequest(participantId, transactionId);
    }

    private void RollbackParticipant(long transactionId, int participantId)
    {
        _communicationManager.SendRollbackRequest(participantId, transactionId);
    }

    private class TwoPhaseTransactionEntry : ITwoPhaseTransactionEntry
    {
        public List<int> ParticipantIds { get; } = [];
    }
}