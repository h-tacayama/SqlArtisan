namespace InlineSqlSharp;

public sealed class SimpleCaseWhenClause(
    SimpleCaseWhenExpr whenExpr,
    CaseThenExpr thenExpr) : AbstractSqlPart
{
    private readonly SimpleCaseWhenExpr _whenExpr = whenExpr;
    private readonly CaseThenExpr _thenExpr = thenExpr;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.WHEN)
        .AppendSpace(_whenExpr)
        .AppendSpace(Keywords.THEN)
        .Append(_thenExpr);
}
