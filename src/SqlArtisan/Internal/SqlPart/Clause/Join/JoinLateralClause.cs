namespace SqlArtisan.Internal;

internal sealed class JoinLateralClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly string _alias;

    internal JoinLateralClause(ISubquery subquery, string alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    // The ON predicate is supplied by the caller's subsequent `.On(...)` step,
    // emitted as a separate OnClause — mirroring INNER JOIN.
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Join} {Keywords.Lateral} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .EncloseInAliasQuotes(_alias);
}
