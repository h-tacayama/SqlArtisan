namespace SqlArtisan;

public abstract class EqualityBasedCondition : SqlCondition
{
    internal abstract SqlExpression LeftSide { get; }

    internal abstract SqlExpression RightSide { get; }
}
