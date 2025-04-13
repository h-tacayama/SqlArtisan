namespace InlineSqlSharp;

public sealed class DateTimeSimpleCaseExpr(
    object expr,
    SimpleCaseWhenClause[] whenClauses,
    CaseElseExpr<DateTimeExpr> elseClause) :
    DateTimeExpr,
    ISimpleCaseExpression
{
    private readonly SimpleCaseExprCore<DateTimeExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
