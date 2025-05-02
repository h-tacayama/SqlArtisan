namespace SqlArtisan;

public sealed class MonthsBetweenFunction : SqlExpression
{
    private readonly SqlExpression _date1;
    private readonly SqlExpression _date2;

    internal MonthsBetweenFunction(SqlExpression date1, SqlExpression date2)
    {
        _date1 = date1;
        _date2 = date2;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.MonthsBetween)
        .OpenParenthesis()
        .Append(_date1)
        .PrependComma(_date2)
        .CloseParenthesis();
}
