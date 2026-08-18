namespace SqlArtisan.Internal;

public sealed class WaitBehavior : LockBehaviorBase
{
    private readonly string _seconds;

    internal WaitBehavior(int seconds)
    {
        _seconds = LockWaitGuard.ValidateSeconds(seconds).ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Wait} ")
        .Append(_seconds);
}
