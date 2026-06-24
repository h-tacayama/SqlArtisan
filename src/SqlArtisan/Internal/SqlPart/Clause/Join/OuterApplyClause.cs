namespace SqlArtisan.Internal;

internal sealed class OuterApplyClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly DerivedTableSchemaBase _alias;

    internal OuterApplyClause(ISubquery subquery, DerivedTableSchemaBase alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Outer} {Keywords.Apply} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
