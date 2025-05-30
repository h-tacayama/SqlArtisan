namespace SqlArtisan.Internal;

public sealed class AddMonthsFunction : SqlExpression
{
    private readonly SqlExpression _dateTime;
    private readonly SqlExpression _months;

    internal AddMonthsFunction(SqlExpression dateTime, SqlExpression months)
    {
        _dateTime = dateTime;
        _months = months;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.AddMonths)
        .OpenParenthesis()
        .Append(_dateTime)
        .PrependComma(_months)
        .CloseParenthesis();
}
