using System.Net;
using System.Net.Sockets;
using System.Text;
using DistributedLockExample.Servers.Abstracts;
using DistributedLockExample.Servers.Enums;

namespace DistributedLockExample.Servers;

public sealed class TcpServer(string ip, int port, int maxThreads) : TcpServerAbstract
{
    private readonly TcpListener _listener = new(IPAddress.Parse(ip), port);
    private readonly Semaphore _maxConcurrentThreads = new(maxThreads, maxThreads);
    private volatile bool _isRunning;

    public override void Start()
    {
        _isRunning = true;
        _listener.Start();
        Console.WriteLine("Server is listening...");

        while (_isRunning)
        {
            while (_listener.Pending())
            {
                _maxConcurrentThreads.WaitOne();
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient!, client);
            }

            Thread.Sleep(100);
        }

        _listener.Stop();
        Console.WriteLine("Server stopped.");
    }

    public override void Stop()
    {
        _isRunning = false;
    }

    private void HandleClient(object obj)
    {
        var client = (TcpClient)obj;
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead <= 0) return;
            var messageType = (TcpServerMessageType)buffer[0];
            var transactionId = BitConverter.ToInt64(buffer, 1);

            Console.WriteLine($"Received message of type {messageType} for transaction {transactionId}");

            byte[] response;

            switch (messageType)
            {
                case TcpServerMessageType.Prepare:
                    response = EncodeVoteResult(true, "Ready to commit");
                    break;
                case TcpServerMessageType.Commit:
                    Console.WriteLine("Committing transaction");
                    response = "Transaction committed"u8.ToArray();
                    break;
                case TcpServerMessageType.Rollback:
                    Console.WriteLine("Rolling back transaction");
                    response = "Transaction rolled back"u8.ToArray();
                    break;
                default:
                    response = "Unknown message type"u8.ToArray();
                    break;
            }

            stream.Write(response, 0, response.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
            _maxConcurrentThreads.Release();
        }
    }

    private byte[] EncodeVoteResult(bool vote, string message)
    {
        var response = new List<byte> { (byte)(vote ? 1 : 0) };
        response.AddRange(Encoding.UTF8.GetBytes(message));
        return response.ToArray();
    }
}
