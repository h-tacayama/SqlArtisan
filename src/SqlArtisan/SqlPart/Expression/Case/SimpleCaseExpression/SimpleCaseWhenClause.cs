namespace SqlArtisan;

public sealed class SimpleCaseWhenClause : SqlPart
{
    private readonly SimpleCaseWhenExpression _whenExpr;
    private readonly CaseThenExpression _thenExpr;

    internal SimpleCaseWhenClause(
        SimpleCaseWhenExpression whenExpr,
        CaseThenExpression thenExpr)
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
