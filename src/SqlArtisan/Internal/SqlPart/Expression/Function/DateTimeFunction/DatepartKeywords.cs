using System.Text;

namespace SqlArtisan.Internal;

// Datepart SQL keywords (e.g. DateTimePart.DayHour -> "DAY_HOUR") are fixed, so
// they are computed once and reused instead of formatting the enum name (which
// calls the relatively expensive Enum.ToString) on every Format.
internal static class DatepartKeywords
{
    private static readonly string[] s_keywords = Build();

    internal static string Of(DateTimePart datepart) => s_keywords[(int)datepart];

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
