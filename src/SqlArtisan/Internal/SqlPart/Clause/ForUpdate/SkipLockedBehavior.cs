namespace SqlArtisan.Internal;

internal sealed class SkipLockedBehavior : LockBehaviorBase
{
    internal SkipLockedBehavior() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.Skip} {Keywords.Locked}");
}
