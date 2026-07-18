namespace SqlArtisan.Internal;

public abstract class ArrayCondition(
    SqlExpression leftSide,
    string @operator,
    SqlExpression rightSide) : SqlCondition
{
    private readonly SqlExpression _leftSide = leftSide;
    private readonly string _operator = @operator;
    private readonly SqlExpression _rightSide = rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(_operator)
        .Append(_rightSide);
}
