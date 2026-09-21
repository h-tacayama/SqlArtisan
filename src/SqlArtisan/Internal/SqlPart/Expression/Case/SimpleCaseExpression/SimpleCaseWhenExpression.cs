using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

public sealed class SimpleCaseWhenExpression : SqlPart, IIncompleteExpression
{
    string IIncompleteExpression.CompletionHint => "Complete the WHEN branch with .Then(...).";

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
