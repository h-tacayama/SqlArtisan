namespace InlineSqlSharp;

public sealed class IsNullCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;

    internal IsNullCondition(AbstractExpr leftSide)
    {
        _leftSide = leftSide;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.IS)
        .Append(Keywords.NULL);
}
