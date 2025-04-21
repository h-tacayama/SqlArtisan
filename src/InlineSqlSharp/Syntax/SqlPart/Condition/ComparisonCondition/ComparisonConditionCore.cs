namespace InlineSqlSharp;

internal sealed class ComparisonConditionCore(
    AbstractExpr leftSide,
    string @operator,
    AbstractExpr rightSide)
{
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly string _operator = @operator;
    private readonly AbstractExpr _rightSide = rightSide;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(_operator)
        .Append(_rightSide);
}
