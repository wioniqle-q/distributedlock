namespace DistributedLock.Enums;

public enum LockMode
{
    NL, // No Lock
    IS, // Intention Shared
    IX, // Intention Exclusive
    S, // Shared
    SIX, // Shared and Intention Exclusive 
    X // Exclusive
}