using System;

public interface ISystemClock
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public class RealClock : ISystemClock
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
