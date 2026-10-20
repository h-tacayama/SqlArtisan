using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

/// <summary>
/// A searched <c>CASE</c> branch awaiting its result: <c>WHEN condition</c>, from
/// <c>Sql.When(condition)</c>; complete it with <see cref="Then(object)"/>.
/// </summary>
public sealed class SearchedCaseWhenCondition : SqlPart, IIncompleteExpression
{
    string IIncompleteExpression.IncompleteMessage =>
        "A CASE WHEN branch requires THEN; complete it with .Then(...).";

    private readonly SqlCondition _whenCondition;

    internal SearchedCaseWhenCondition(SqlCondition whenCondition)
    {
        _whenCondition = whenCondition;
    }

    /// <summary>
    /// Completes the branch: <c>WHEN condition THEN result</c>.
    /// </summary>
    /// <param name="thenExpr">The branch's result value or expression.</param>
    public SearchedCaseWhenClause Then(object thenExpr) =>
        new(this, new CaseThenExpression(Resolve(thenExpr)));

    internal override bool IsEmpty => _whenCondition.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) =>
        _whenCondition.Format(buffer);
}
