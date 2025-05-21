namespace SqlArtisan.Internal;

public sealed class IsNotNullCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;

    internal IsNotNullCondition(SqlExpression leftSide)
    {
        _leftSide = leftSide;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Is} {Keywords.Not} ")
        .Append(Keywords.Null);
}
