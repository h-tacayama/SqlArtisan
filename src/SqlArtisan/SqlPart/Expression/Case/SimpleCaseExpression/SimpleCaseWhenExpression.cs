using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

/// <summary>
/// A simple <c>CASE</c> branch awaiting its result: <c>WHEN value</c>, from
/// <c>Sql.When(value)</c>; complete it with <see cref="Then(object)"/>.
/// </summary>
public sealed class SimpleCaseWhenExpression : SqlPart, IIncompleteExpression
{
    string IIncompleteExpression.IncompleteMessage =>
        "A CASE WHEN branch requires THEN; complete it with .Then(...).";

    private readonly SqlExpression _whenExpr;

    internal SimpleCaseWhenExpression(SqlExpression whenExpr)
    {
        _whenExpr = whenExpr;
    }

    /// <summary>
    /// Completes the branch: <c>WHEN value THEN result</c>.
    /// </summary>
    /// <param name="thenExpr">The branch's result value or expression.</param>
    public SimpleCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpression(Resolve(thenExpr)));

    internal override void Format(SqlBuildingBuffer buffer) =>
        _whenExpr.Format(buffer);
}
