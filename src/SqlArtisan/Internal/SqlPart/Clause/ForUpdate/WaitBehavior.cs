namespace SqlArtisan.Internal;

public sealed class WaitBehavior(int seconds) : LockBehaviorBase
{
    private readonly int _seconds = seconds;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.Wait} {_seconds}");
}
