namespace SqlArtisan.Internal;

public sealed class NowaitBehavior : LockBehaviorBase
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Nowait);
}
