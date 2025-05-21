namespace SqlArtisan.Internal;

public abstract class ArithmeticOperator(
    SqlExpression leftSide,
    string @operator,
    SqlExpression rightSide) : SqlExpression
{
    private readonly SqlExpression _leftSide = leftSide;
    private readonly string _operator = @operator;
    private readonly SqlExpression _rightSide = rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis(_leftSide)
        .EncloseInSpaces(_operator)
        .CloseParenthesis(_rightSide);
}
