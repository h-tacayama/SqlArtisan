namespace InlineSqlSharp;

public sealed class CharacterSimpleCaseExpr(
    object expr,
    SimpleCaseWhenClause[] whenClauses,
    CaseElseExpr<CharacterExpr> elseClause) :
    CharacterExpr,
    ISimpleCaseExpression
{
    private readonly SimpleCaseExprCore<CharacterExpr> _core =
        new(expr, whenClauses, elseClause);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
