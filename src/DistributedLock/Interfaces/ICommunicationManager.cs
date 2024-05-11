using DistributedLock.Managers;
using DistributedLock.Structs;

namespace DistributedLock.Interfaces;

public interface ICommunicationManager
{
    public static CommunicationManager Instance => null!;
    public void Connect(int participantId, string ip, int port);
    public void Disconnect(int participantId);
    public VoteResult SendPrepareRequest(int participantId, long transactionId);
    public void SendCommitRequest(int participantId, long transactionId);
    public void SendRollbackRequest(int participantId, long transactionId);
}