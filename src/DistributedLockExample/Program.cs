using System.Net.Sockets;
using DistributedLock.Enums;
using DistributedLock.Interfaces;
using DistributedLock.Managers;
using DistributedLockExample.Servers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DistributedLockExample;

public static class Program
{
    private const string ServerIp = "127.0.0.1"; // IP to run the server on
    private const int ServerPort = 12345; // Port to run the server on
    private const int MaxThreads = 100 * 1000; // Number of threads to run
    private const int TransactionCount = 100 * 1000; // Number of transactions to run 

    public static async Task Main(string[] _)
    {
        var server = new TcpServer(ServerIp, ServerPort, MaxThreads);
        var serverThread = new Thread(server.Start);
        serverThread.Start();

        var mongoClient = new MongoClient("");
        var database = mongoClient.GetDatabase("");
        var collection = database.GetCollection<BsonDocument>("");

        var lockManager = new DistributedLockManager();
        var twoPhaseCommit = new DistributedTwoPhaseCommit();
        var timestampManager = new TimestampManager();

        var task1 = StartTransactions(lockManager, twoPhaseCommit, timestampManager, collection);
        await task1;

        serverThread.Join();
        server.Stop();
    }

    private static async Task StartTransactions(IDistributedLockManager distributedLockManager,
        IDistributedTwoPhaseCommit distributedTwoPhaseCommit, ITimestampManager timestampManager,
        IMongoCollection<BsonDocument> mongoCollection)
    {
        for (var transactionId = 0; transactionId < TransactionCount; transactionId++)
            await StartTransaction(distributedLockManager, distributedTwoPhaseCommit, timestampManager, mongoCollection,
                transactionId);
    }

    private static async Task StartTransaction(IDistributedLockManager distributedLockManager,
        IDistributedTwoPhaseCommit distributedTwoPhaseCommit, ITimestampManager timestampManager,
        IMongoCollection<BsonDocument> mongoCollection, int transactionId)
    {
        var communicationManager = CommunicationManager.Instance;

        var participantId = GuidToInt(Guid.NewGuid());

        try
        {
            communicationManager.Connect(participantId, ServerIp, ServerPort);
            Console.WriteLine($"Participant {participantId} connected.");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error connecting participant {participantId}: {ex.Message}");
            return;
        }

        distributedTwoPhaseCommit.AddTransaction(transactionId);

        if (distributedLockManager.AcquireLock(transactionId, participantId, LockMode.X) is false)
        {
            Console.WriteLine($"Failed to acquire lock for participant {participantId} in transaction {transactionId}");
            return;
        }

        Console.WriteLine($"Lock acquired for participant {participantId} in transaction {transactionId}");

        timestampManager.AssignTimestamp(transactionId);

        var participants = new List<int> { participantId };
        var commitSuccess = distributedTwoPhaseCommit.Commit(transactionId, participants);

        if (commitSuccess)
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("", "");
                var document = await mongoCollection.Find(filter).FirstOrDefaultAsync();
                Console.WriteLine($"Data read from MongoDB for transaction {transactionId}: {document}");
                Console.WriteLine($"Transaction {transactionId} committed successfully.");
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"Error reading from MongoDB for transaction {transactionId}: {ex.Message}");
            }
        else
            Console.WriteLine($"Transaction {transactionId} failed to commit.");

        distributedLockManager.ReleaseLock(transactionId, participantId, LockMode.X);
        communicationManager.Disconnect(participantId);
        timestampManager.RemoveTimestamp(transactionId);
    }

    private static int GuidToInt(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return BitConverter.ToInt32(bytes, 0);
    }
}