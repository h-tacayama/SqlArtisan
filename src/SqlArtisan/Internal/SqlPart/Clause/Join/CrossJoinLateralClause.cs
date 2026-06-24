namespace SqlArtisan.Internal;

internal sealed class CrossJoinLateralClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly UntypedDerivedTable _alias;

    internal CrossJoinLateralClause(ISubquery subquery, UntypedDerivedTable alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Cross} {Keywords.Join} {Keywords.Lateral} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
