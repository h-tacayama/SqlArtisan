namespace SqlArtisan.Internal;

public sealed class EmptyCondition : SqlCondition
{
    internal EmptyCondition() { }

    internal override bool IsEmpty => true;

    internal override void Format(SqlBuildingBuffer buffer) { }
}
