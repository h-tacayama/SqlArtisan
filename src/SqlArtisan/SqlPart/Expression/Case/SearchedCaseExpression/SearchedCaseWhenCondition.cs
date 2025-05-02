using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public sealed class SearchedCaseWhenCondition : SqlPart
{
    private readonly SqlCondition _whenCondition;

    internal SearchedCaseWhenCondition(SqlCondition whenCondition)
    {
        _whenCondition = whenCondition;
    }

    public SearchedCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpression(Resolve(thenExpr)));

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _whenCondition.FormatSql(buffer);
}
