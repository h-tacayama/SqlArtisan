namespace SqlArtisan.Internal;

public sealed class WaitBehavior(int seconds) : LockBehaviorBase
{
    private readonly string _seconds = LockWaitGuard.ValidateSeconds(seconds).ToInvariantString();

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Wait} ")
        .Append(_seconds);
}
