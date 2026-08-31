namespace SqlArtisan.Internal;

internal sealed class JoinUsingClause : SqlPart
{
    private readonly DbColumn[] _columns;

    // paramName: the callers pre-check their params tail, so the only null
    // element reaching here is the lead column — report its public name.
    internal JoinUsingClause(DbColumn[] columns, string paramName)
    {
        CollectionGuard.ThrowIfNullElement(
            columns, paramName, "A USING column list must not contain a null column.");
        _columns = columns;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .OpenParenthesis()
        .AppendUnqualifiedColumnsCsv(_columns)
        .CloseParenthesis();
}
