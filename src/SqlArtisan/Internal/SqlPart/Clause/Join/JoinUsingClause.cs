namespace SqlArtisan.Internal;

internal sealed class JoinUsingClause(DbColumn[] columns) : SqlPart
{
    private readonly DbColumn[] _columns = columns;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .OpenParenthesis()
        .AppendUnqualifiedColumnsCsv(_columns)
        .CloseParenthesis();
}
