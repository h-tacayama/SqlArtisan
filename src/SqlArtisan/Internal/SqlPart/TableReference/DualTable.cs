namespace SqlArtisan.Internal;

public sealed class DualTable : TableReference
{
    internal DualTable() : base(Keywords.Dual) { }

    internal override string CorrelationName => "";
}
