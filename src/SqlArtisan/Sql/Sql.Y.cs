using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>YEAR</c> interval field, for the sole-field overload of
    /// <see cref="IntervalLiteral(string, IntervalField)"/> or as the leading
    /// field of <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (e.g. <c>YEAR TO MONTH</c>).
    /// </summary>
    /// <param name="precision">The leading field's digit count (0-9); omit for
    /// Oracle's own default of 2.</param>
    /// <returns>An <see cref="IntervalField"/> emitting <c>YEAR</c> or <c>YEAR(precision)</c>.</returns>
    public static IntervalField Year(int? precision = null) => new(DateTimePart.Year, precision);
}
