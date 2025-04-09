namespace InlineSqlSharp;

public sealed class DateDiffSubtractionOperator(
    DateTimeExpr leftSide,
    DateTimeExpr rightSide) : NumericExpr
{
    private readonly DateTimeExpr _leftSide = leftSide;
    private readonly DateTimeExpr _rightSide = rightSide;

    public override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis(_leftSide)
        .EncloseInSpaces(Operators.Minus)
        .CloseParenthesis(_rightSide);
}
