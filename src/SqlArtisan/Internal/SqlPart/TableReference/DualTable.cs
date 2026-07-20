namespace SqlArtisan.Internal;

public sealed class DualTable : TableReference
{
    internal DualTable() : base(Keywords.Dual) { }

    internal override string CorrelationName => "";

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Dual);
}
