using System.Text;

namespace SqlArtisan.Internal;

// Datepart SQL keywords (e.g. DateTimePart.DayHour -> "DAY_HOUR") are fixed, so
// they are computed once and reused instead of formatting the enum name (which
// calls the relatively expensive Enum.ToString) on every Format.
internal static class DatepartKeywords
{
    private static readonly string[] s_keywords = Build();

    // PostgreSQL matches an expression index by the literal as written, so
    // DATE_TRUNC('MONTH', ts) skips an index built on the manual's 'month' (#661).
    private static readonly string[] s_lowerKeywords =
        [.. s_keywords.Select(keyword => keyword.ToLowerInvariant())];

    internal static string Of(DateTimePart datepart) => s_keywords[IndexOf(datepart)];

    // A factory resolves its caller's value at the call (#661), so a cast-in
    // undefined one fails there, naming the construct and the caller's parameter.
    internal static string Resolve(DateTimePart datepart, string construct, string paramName) =>
        s_keywords[IndexOf(datepart, construct, paramName)];

    internal static string ResolveLower(
        DateTimePart datepart,
        string construct,
        string paramName) =>
        s_lowerKeywords[IndexOf(datepart, construct, paramName)];

    // A cast-in undefined value would otherwise index past the table and surface
    // as a bare IndexOutOfRangeException.
    private static int IndexOf(DateTimePart datepart) =>
        (uint)datepart < (uint)s_keywords.Length
            ? (int)datepart
            : throw new ArgumentOutOfRangeException(
                nameof(datepart), datepart, "The value is not a defined DateTimePart.");

    private static int IndexOf(DateTimePart datepart, string construct, string paramName) =>
        (uint)datepart < (uint)s_keywords.Length
            ? (int)datepart
            : throw new ArgumentOutOfRangeException(
                paramName, datepart, $"{construct} requires a defined DateTimePart value.");

    private static string[] Build()
    {
        DateTimePart[] values = Enum.GetValues<DateTimePart>();
        string[] keywords = new string[values.Length];

        foreach (DateTimePart value in values)
        {
            keywords[(int)value] = ToUpperSnakeCase(value.ToString());
        }

        return keywords;
    }

    private static string ToUpperSnakeCase(string name)
    {
        StringBuilder result = new(name.Length + 4);

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (i > 0 && char.IsUpper(c))
            {
                result.Append('_');
            }

            result.Append(char.ToUpperInvariant(c));
        }

        return result.ToString();
    }
}
