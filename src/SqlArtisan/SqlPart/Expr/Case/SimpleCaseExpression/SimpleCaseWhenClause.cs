namespace SqlArtisan;

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
        .Append($"{Keywords.When} ")
        .Append(_whenExpr)
        .Append($" {Keywords.Then} ")
        .Append(_thenExpr);
}
