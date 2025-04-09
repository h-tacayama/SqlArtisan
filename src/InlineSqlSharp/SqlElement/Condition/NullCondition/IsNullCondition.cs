namespace InlineSqlSharp;

public sealed class IsNullCondition(IExpr leftSide) : ICondition
{
    private readonly IExpr _leftSide = leftSide;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.IS)
        .Append(Keywords.NULL);
}
