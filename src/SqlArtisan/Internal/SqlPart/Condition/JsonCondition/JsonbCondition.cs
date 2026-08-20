namespace SqlArtisan.Internal;

public abstract class JsonbCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly string _operator;
    private readonly SqlExpression _rightSide;

    private protected JsonbCondition(SqlExpression leftSide, string @operator, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _operator = @operator;
        _rightSide = rightSide;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(_operator)
        .Append(_rightSide);
}
