namespace SqlArtisan;

internal abstract class ArithmeticOperator(
    AbstractExpr leftSide,
    string @operator,
    AbstractExpr rightSide) : AbstractExpr
{
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly string _operator = @operator;
    private readonly AbstractExpr _rightSide = rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis(_leftSide)
        .EncloseInSpaces(_operator)
        .CloseParenthesis(_rightSide);
}
