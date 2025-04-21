namespace InlineSqlSharp;

public sealed class IsNullCondition(AbstractExpr leftSide) : AbstractCondition
{
    private readonly AbstractExpr _leftSide = leftSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.IS)
        .Append(Keywords.NULL);
}
