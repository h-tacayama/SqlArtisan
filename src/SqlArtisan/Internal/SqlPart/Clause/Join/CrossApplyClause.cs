namespace SqlArtisan.Internal;

internal sealed class CrossApplyClause : SqlPart
{
    private readonly SqlPartAgent _subquery;
    private readonly string _alias;

    internal CrossApplyClause(ISubquery subquery, string alias)
    {
        _subquery = new(subquery.Format);
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Cross} {Keywords.Apply} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .EncloseInAliasQuotes(_alias);
}
