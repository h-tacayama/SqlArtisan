namespace SqlArtisan.Internal;

internal sealed class CrossApplyClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly DerivedTableBase _alias;

    internal CrossApplyClause(ISubquery subquery, DerivedTableBase alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Cross} {Keywords.Apply} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
