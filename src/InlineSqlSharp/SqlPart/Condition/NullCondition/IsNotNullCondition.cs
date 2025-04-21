namespace InlineSqlSharp;

public sealed class IsNotNullCondition(AbstractExpr leftSide) : AbstractCondition
{
    private readonly AbstractExpr _leftSide = leftSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.IS)
        .AppendSpace(Keywords.NOT)
        .Append(Keywords.NULL);
}
