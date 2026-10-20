namespace SqlArtisan.Internal;

internal sealed class NowaitBehavior : LockBehaviorBase
{
    internal NowaitBehavior() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Nowait);
}
