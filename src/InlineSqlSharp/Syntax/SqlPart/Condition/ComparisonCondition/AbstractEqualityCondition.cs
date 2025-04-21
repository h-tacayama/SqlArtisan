namespace InlineSqlSharp;

public abstract class AbstractEqualityCondition : AbstractCondition
{
    internal abstract AbstractExpr LeftSide { get; }

    internal abstract AbstractExpr RightSide { get; }
}
