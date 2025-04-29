namespace InlineSqlSharp;

public sealed class SearchedCaseWhenCondition : AbstractSqlPart
{
    private readonly AbstractCondition _whenCondition;

    internal SearchedCaseWhenCondition(AbstractCondition whenCondition)
    {
        _whenCondition = whenCondition;
    }

    public SearchedCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpr(ExprResolver.Resolve(thenExpr)));

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _whenCondition.FormatSql(buffer);
}
