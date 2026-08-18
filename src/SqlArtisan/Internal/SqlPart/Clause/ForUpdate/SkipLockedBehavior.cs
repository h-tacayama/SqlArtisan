namespace SqlArtisan.Internal;

public sealed class SkipLockedBehavior : LockBehaviorBase
{
    internal SkipLockedBehavior() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.Skip} {Keywords.Locked}");
}
