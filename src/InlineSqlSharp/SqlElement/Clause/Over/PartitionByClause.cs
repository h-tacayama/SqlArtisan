namespace InlineSqlSharp;

public sealed class PartitionByClause(IExpr[] expressions) : ISqlElement
{
    private readonly IExpr[] _expressions = expressions;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.PARTITION)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy ORDER_BY(
        params IExprOrAliasOrSortOrder[] sortExpressions) =>
        new(this, new OrderByClause(sortExpressions));
}
