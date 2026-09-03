namespace SqlArtisan.Internal;

// NUMTOYMINTERVAL/NUMTODSINTERVAL's interval_unit domains are fixed by Oracle's
// own function definitions (ADR 0012): no other engine has either function, so
// any other DateTimePart is invalid on every supported dialect.
internal static class NumToIntervalGuard
{
    internal static void ValidateYearMonthUnit(DateTimePart unit)
    {
        if (unit is not (DateTimePart.Year or DateTimePart.Month))
        {
            throw new ArgumentException(
                $"{Keywords.Numtoyminterval} requires an interval unit of YEAR or MONTH.");
        }
    }

    internal static void ValidateDaySecondUnit(DateTimePart unit)
    {
        if (unit is not (DateTimePart.Day or DateTimePart.Hour
            or DateTimePart.Minute or DateTimePart.Second))
        {
            throw new ArgumentException(
                $"{Keywords.Numtodsinterval} requires an interval unit of DAY, HOUR, MINUTE, or SECOND.");
        }
    }
}
