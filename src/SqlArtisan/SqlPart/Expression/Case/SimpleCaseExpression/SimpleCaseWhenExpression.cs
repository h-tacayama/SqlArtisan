using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public sealed class SimpleCaseWhenExpression : SqlPart
{
    private readonly SqlExpression _whenExpr;

    internal SimpleCaseWhenExpression(SqlExpression whenExpr)
    {
        _whenExpr = whenExpr;
    }

    public SimpleCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpression(Resolve(thenExpr)));

    internal override void Format(SqlBuildingBuffer buffer) =>
        _whenExpr.Format(buffer);
}
