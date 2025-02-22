namespace InlineSqlSharp;

public abstract class ArithmeticOperator(
	NumericExpr leftSide,
	string @operator,
	NumericExpr rightSide) : NumericExpr
{
	private readonly NumericExpr _leftSide = leftSide;
	private readonly string _operator = @operator;
	private readonly NumericExpr _rightSide = rightSide;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.OpenParenthesis(_leftSide)
		.EncloseInSpaces(_operator)
		.CloseParenthesis(_rightSide);
}
