namespace SqlArtisan.Internal;

public sealed class LastDayFunction : SqlExpression
{
    private readonly SqlExpression _date;

    internal LastDayFunction(SqlExpression date)
    {
        _date = date;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LastDay)
        .OpenParenthesis()
        .Append(_date)
        .CloseParenthesis();
}
