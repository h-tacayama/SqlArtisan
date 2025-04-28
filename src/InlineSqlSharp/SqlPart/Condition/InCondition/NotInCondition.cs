namespace InlineSqlSharp;

public sealed class NotInCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly AbstractExpr[] _expressions;

    internal NotInCondition(AbstractExpr leftSide, AbstractExpr[] expressions)
    {
        _leftSide = leftSide;
        _expressions = expressions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.In} ")
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
