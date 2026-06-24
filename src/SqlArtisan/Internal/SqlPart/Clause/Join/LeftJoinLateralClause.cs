namespace SqlArtisan.Internal;

internal sealed class LeftJoinLateralClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly string _alias;

    internal LeftJoinLateralClause(ISubquery subquery, string alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    // A LEFT JOIN LATERAL requires an ON predicate; with no extra correlation
    // beyond the subquery body, the idiomatic always-true `ON true` is emitted.
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Left} {Keywords.Join} {Keywords.Lateral} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .EncloseInAliasQuotes(_alias)
        .Append($" {Keywords.On} {Keywords.True}");
}
