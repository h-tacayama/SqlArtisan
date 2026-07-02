namespace SqlArtisan.Internal;

internal sealed class LeftJoinLateralClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly DerivedTableBase _alias;

    internal LeftJoinLateralClause(ISubquery subquery, DerivedTableBase alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    // A LEFT JOIN LATERAL requires an ON predicate; with no extra correlation
    // beyond the subquery body, the idiomatic always-true `ON TRUE` is emitted.
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Left} {Keywords.Join} {Keywords.Lateral}").AppendSpace()
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias)
        .AppendSpace().Append($"{Keywords.On} {Keywords.True}");
}
