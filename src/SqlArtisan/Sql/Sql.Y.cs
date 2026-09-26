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
    /// <returns>An <see cref="IntervalField"/> emitting <c>YEAR</c>.</returns>
    public static IntervalField Year() => new(DateTimePart.Year, null);

    /// <inheritdoc cref="Year()"/>
    /// <param name="precision">The leading field's digit count (0-9); the no-argument
    /// overload leaves Oracle's own default of 2.</param>
    /// <returns>An <see cref="IntervalField"/> emitting <c>YEAR(precision)</c>.</returns>
    public static IntervalField Year(int precision) =>
        new(DateTimePart.Year, precision);
}
