namespace SqlArtisan;

public sealed class IsNullCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;

    internal IsNullCondition(SqlExpression leftSide)
    {
        _leftSide = leftSide;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Is} {Keywords.Null}");
}
