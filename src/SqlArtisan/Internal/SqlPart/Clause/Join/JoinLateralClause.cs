namespace SqlArtisan.Internal;

internal sealed class JoinLateralClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly DerivedTableBase _alias;

    internal JoinLateralClause(ISubquery subquery, DerivedTableBase alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    // The ON predicate is supplied by the caller's subsequent `.On(...)` step,
    // emitted as a separate OnClause — mirroring INNER JOIN.
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Join} {Keywords.Lateral}").AppendSpace()
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
