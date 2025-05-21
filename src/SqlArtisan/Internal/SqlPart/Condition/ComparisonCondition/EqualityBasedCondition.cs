namespace SqlArtisan.Internal;

public abstract class EqualityBasedCondition : SqlCondition
{
    internal abstract SqlExpression LeftSide { get; }

    internal abstract SqlExpression RightSide { get; }
}
