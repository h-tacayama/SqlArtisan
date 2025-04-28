namespace InlineSqlSharp;

public sealed class IsNotNullCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;

    internal IsNotNullCondition(AbstractExpr leftSide)
    {
        _leftSide = leftSide;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Is} {Keywords.Not} ")
        .Append(Keywords.Null);
}
