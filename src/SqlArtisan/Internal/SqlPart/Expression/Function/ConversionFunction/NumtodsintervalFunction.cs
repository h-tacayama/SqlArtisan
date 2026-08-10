namespace SqlArtisan.Internal;

public sealed class NumtodsintervalFunction : SqlExpression
{
    private readonly SqlExpression _n;
    private readonly DateTimePart _unit;

    internal NumtodsintervalFunction(SqlExpression n, DateTimePart unit)
    {
        NumToIntervalGuard.ValidateDaySecondUnit(unit);
        _n = n;
        _unit = unit;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Numtodsinterval)
        .OpenParenthesis()
        .Append(_n)
        .Append(", ")
        .AppendStringLiteral(DatepartKeywords.Of(_unit))
        .CloseParenthesis();
}
