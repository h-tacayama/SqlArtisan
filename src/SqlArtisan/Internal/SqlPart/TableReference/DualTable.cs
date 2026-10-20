namespace SqlArtisan.Internal;

internal sealed class DualTable : TableReference
{
    internal DualTable() : base(Keywords.Dual, "A table requires a name.") { }

    internal override string CorrelationName => "";
}
