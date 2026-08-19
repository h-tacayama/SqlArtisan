namespace SqlArtisan;

/// <summary>
/// The condition produced by <c>==</c> and <c>!=</c> on
/// <see cref="SqlExpression"/>; the element type of the assignment
/// arrays accepted by <c>Set</c>, <c>DoUpdateSet</c>,
/// <c>ThenUpdateSet</c>, and <c>OnDuplicateKeyUpdate</c>.
/// </summary>
public abstract class EqualityBasedCondition : SqlCondition
{
    internal abstract SqlExpression LeftSide { get; }

    internal abstract SqlExpression RightSide { get; }
}
