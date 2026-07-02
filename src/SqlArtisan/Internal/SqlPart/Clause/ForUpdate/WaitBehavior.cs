namespace SqlArtisan.Internal;

public sealed class WaitBehavior(int seconds) : LockBehaviorBase
{
    private readonly string _seconds = seconds.ToInvariantString();

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Wait)
        .AppendSpace()
        .Append(_seconds);
}
