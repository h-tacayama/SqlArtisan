namespace InlineSqlSharp;

public sealed class SearchedCaseWhenCondition(AbstractCondition whenCondition) :
    AbstractSqlPart
{
    private readonly AbstractCondition _whenCondition = whenCondition;

    public SearchedCaseWhenClause THEN(object thenExpr) =>
        new(this, new CaseThenExpr(ExprRsolver.Resolve(thenExpr)));

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _whenCondition.FormatSql(buffer);
}
