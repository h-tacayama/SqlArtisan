namespace SqlArtisan.Internal;

public abstract class BinaryOperator : SqlExpression
{
    private readonly SqlExpression _leftSide;
    private readonly string _operator;
    private readonly SqlExpression _rightSide;

    private protected BinaryOperator(SqlExpression leftSide, string @operator, SqlExpression rightSide)
    {
        _leftSide = leftSide;
        _operator = @operator;
        _rightSide = rightSide;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis(_leftSide)
        .EncloseInSpaces(_operator)
        .CloseParenthesis(_rightSide);
}
