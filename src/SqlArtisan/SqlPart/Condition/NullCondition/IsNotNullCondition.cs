namespace SqlArtisan;

public sealed class IsNotNullCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;

    internal IsNotNullCondition(SqlExpression leftSide)
    {
        _leftSide = leftSide;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Is} {Keywords.Not} ")
        .Append(Keywords.Null);
}
