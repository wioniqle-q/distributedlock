namespace DistributedLockExample.Servers.Enums;

public enum TcpServerMessageType : byte
{
    Prepare,
    Commit,
    Rollback
}