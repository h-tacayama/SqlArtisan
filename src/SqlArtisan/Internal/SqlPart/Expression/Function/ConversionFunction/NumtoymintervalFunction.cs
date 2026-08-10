namespace SqlArtisan.Internal;

public sealed class NumtoymintervalFunction : SqlExpression
{
    private readonly SqlExpression _n;
    private readonly DateTimePart _unit;

    internal NumtoymintervalFunction(SqlExpression n, DateTimePart unit)
    {
        NumToIntervalGuard.ValidateYearMonthUnit(unit);
        _n = n;
        _unit = unit;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Numtoyminterval)
        .OpenParenthesis()
        .Append(_n)
        .Append(", ")
        .AppendStringLiteral(DatepartKeywords.Of(_unit))
        .CloseParenthesis();
}
