namespace InlineSqlSharp;

internal sealed class GroupByClause(IExpr[] groupingExpressions) : ISqlElement
{
    private readonly IExpr[] _groupingExpressions = groupingExpressions;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.GROUP)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_groupingExpressions);
}
