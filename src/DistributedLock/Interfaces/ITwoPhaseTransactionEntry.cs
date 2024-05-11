namespace DistributedLock.Interfaces;

internal interface ITwoPhaseTransactionEntry
{
    internal List<int> ParticipantIds { get; }
}