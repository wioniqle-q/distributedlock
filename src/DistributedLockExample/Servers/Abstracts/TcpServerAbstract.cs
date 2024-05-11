using DistributedLockExample.Servers.Interfaces;

namespace DistributedLockExample.Servers.Abstracts;

public abstract class TcpServerAbstract : ITcpServer
{
    public abstract void Start();
    public abstract void Stop();
}