namespace SqlArtisan.Internal;

internal sealed class JoinUsingClause : SqlPart
{
    private readonly DbColumn[] _columns;

    internal JoinUsingClause(DbColumn[] columns)
    {
        CollectionGuard.ThrowIfNullElement(
            columns, nameof(columns), "A USING column list must not contain a null column.");
        _columns = columns;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .OpenParenthesis()
        .AppendUnqualifiedColumnsCsv(_columns)
        .CloseParenthesis();
}
