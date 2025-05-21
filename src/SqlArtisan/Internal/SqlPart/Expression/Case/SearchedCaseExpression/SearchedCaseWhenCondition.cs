using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

public sealed class SearchedCaseWhenCondition : SqlPart
{
    private readonly SqlCondition _whenCondition;

    internal SearchedCaseWhenCondition(SqlCondition whenCondition)
    {
        _whenCondition = whenCondition;
    }

    public SearchedCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpression(Resolve(thenExpr)));

    internal override void Format(SqlBuildingBuffer buffer) =>
        _whenCondition.Format(buffer);
}
