namespace SqlArtisan.Internal;

public sealed class NowaitBehavior : LockBehaviorBase
{
    internal NowaitBehavior() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Nowait);
}
