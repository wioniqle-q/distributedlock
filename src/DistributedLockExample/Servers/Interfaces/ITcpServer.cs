namespace DistributedLockExample.Servers.Interfaces;

internal interface ITcpServer
{
    public void Start();
    public void Stop();
}