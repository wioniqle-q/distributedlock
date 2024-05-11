using DistributedLock.Interfaces;
using DistributedLock.Structs;

namespace DistributedLock.Abstracts;

public abstract class CommunicationManagerAbstract : ICommunicationManager
{
    public abstract void Connect(int participantId, string ip, int port);
    public abstract void Disconnect(int participantId);
    public abstract VoteResult SendPrepareRequest(int participantId, long transactionId);
    public abstract void SendCommitRequest(int participantId, long transactionId);
    public abstract void SendRollbackRequest(int participantId, long transactionId);
}