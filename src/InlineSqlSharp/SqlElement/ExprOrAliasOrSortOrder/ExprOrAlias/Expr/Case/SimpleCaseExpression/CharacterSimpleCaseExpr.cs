namespace InlineSqlSharp;

public sealed class CharacterSimpleCaseExpr<TComparisonExpr>(
    TComparisonExpr expr,
    SimpleCaseWhenClause<TComparisonExpr, CharacterExpr>[] whenClauses,
    CaseElseExpr<CharacterExpr> elseClause) :
    CharacterExpr,
    ISimpleCaseExpression
    where TComparisonExpr : IDataTypeExpr
{
    private readonly SimpleCaseExprCore<TComparisonExpr, CharacterExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
