using System.Text;

namespace SqlArtisan.Internal;

// Datepart SQL keywords (e.g. Datepart.DayHour -> "DAY_HOUR") are fixed, so they
// are computed once and reused instead of formatting the enum name (which calls
// the relatively expensive Enum.ToString) on every Format.
internal static class DatepartKeywords
{
    private static readonly string[] s_keywords = Build();

    internal static string Of(Datepart datepart) => s_keywords[(int)datepart];

    private static string[] Build()
    {
        Datepart[] values = Enum.GetValues<Datepart>();
        string[] keywords = new string[values.Length];

        foreach (Datepart value in values)
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
