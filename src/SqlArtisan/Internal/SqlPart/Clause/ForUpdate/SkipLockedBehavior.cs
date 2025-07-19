namespace SqlArtisan.Internal;

public sealed class SkipLockedBehavior : LockBehaviorBase
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.Skip} {Keywords.Locked}");
}
