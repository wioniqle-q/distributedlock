using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using DistributedLock.Abstracts;
using DistributedLock.Enums;
using DistributedLock.Structs;

namespace DistributedLock.Managers;

public sealed class CommunicationManager : CommunicationManagerAbstract
{
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private readonly SemaphoreSlim _clientsSemaphore = new(1, 3_000_000);
    private const int BufferSize = 1024; 
    
    private CommunicationManager()
    {
    }

    public static CommunicationManager Instance { get; } = new();

    public override void Connect(int participantId, string ip, int port)
    {
        _clientsSemaphore.Wait();
        try
        {
            if (_clients.ContainsKey(participantId))
                throw new Exception($"Participant {participantId} is already connected.");

            var client = new TcpClient();
            client.Connect(ip, port);
            _clients[participantId] = client;
        }
        finally
        {
            _clientsSemaphore.Release();
        }
    }

    public override void Disconnect(int participantId)
    {
        _clientsSemaphore.Wait();
        try
        {
            if (_clients.TryGetValue(participantId, out var client) is false) return;

            client.Close();
            _clients.TryRemove(participantId, out _);
        }
        finally
        {
            _clientsSemaphore.Release();
        }
    }

    public override VoteResult SendPrepareRequest(int participantId, long transactionId)
    {
        if (_clients.TryGetValue(participantId, out var client) is false)
            throw new Exception($"Participant {participantId} is not connected.");

        var stream = client.GetStream();

        var prepareMessage = EncodeMessage(CommunicationMessageType.Prepare, transactionId);
        stream.Write(prepareMessage, 0, prepareMessage.Length);

        var response = ReadResponse(stream);
        return DecodeVoteResult(response);
    }

    public override void SendCommitRequest(int participantId, long transactionId)
    {
        if (_clients.TryGetValue(participantId, out var client) is false)
            throw new Exception($"Participant {participantId} is not connected.");

        var stream = client.GetStream();

        var commitMessage = EncodeMessage(CommunicationMessageType.Commit, transactionId);
        stream.Write(commitMessage, 0, commitMessage.Length);
    }

    public override void SendRollbackRequest(int participantId, long transactionId)
    {
        if (_clients.TryGetValue(participantId, out var client) is false)
            throw new Exception($"Participant {participantId} is not connected.");

        var stream = client.GetStream();

        var rollbackMessage = EncodeMessage(CommunicationMessageType.Rollback, transactionId);
        var prepareMessage = EncodeMessage(CommunicationMessageType.Prepare, transactionId);
        stream.Write(rollbackMessage, 0, prepareMessage.Length);
    }
    
    private static byte[] EncodeMessage(CommunicationMessageType communicationMessageType, long transactionId)
    {
        var buffer = new byte[9];
        buffer[0] = (byte)communicationMessageType;
    
        Span<byte> transactionIdBytes = stackalloc byte[8];
        BitConverter.TryWriteBytes(transactionIdBytes, transactionId);
        transactionIdBytes.CopyTo(buffer.AsSpan(1));

        return buffer;
    }
    
    private static byte[] ReadResponse(NetworkStream stream)
    {
        var buffer = new byte[BufferSize];
        var totalBytesRead = 0;
        var bufferSize = BufferSize;

        do
        {
            var bytesRead = stream.Read(buffer, totalBytesRead, bufferSize - totalBytesRead);
            if (bytesRead is 0)
                break;

            totalBytesRead += bytesRead;
            
            if (totalBytesRead != bufferSize) continue;
            bufferSize *= 2; 
            
            var newBuffer = new byte[bufferSize];
            
            buffer.AsSpan(0, totalBytesRead).CopyTo(newBuffer);
            buffer = newBuffer;
        }
        while (true);

        return buffer.AsSpan(0, totalBytesRead).ToArray();
    }
    
    private static VoteResult DecodeVoteResult(byte[] response)
    {
        var vote = response[0] is 1;
        var message = Encoding.UTF8.GetString(response, 1, response.Length - 1);
        return vote ? VoteResult.Yes(message) : VoteResult.No(message);
    }
}
