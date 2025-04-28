namespace InlineSqlSharp;

public sealed class SimpleCaseWhenClause : AbstractSqlPart
{
    private readonly SimpleCaseWhenExpr _whenExpr;
    private readonly CaseThenExpr _thenExpr;

    internal SimpleCaseWhenClause(
        SimpleCaseWhenExpr whenExpr,
        CaseThenExpr thenExpr)
    {
        _whenExpr = whenExpr;
        _thenExpr = thenExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.When)
        .AppendSpace(_whenExpr)
        .AppendSpace(Keywords.Then)
        .Append(_thenExpr);
}
