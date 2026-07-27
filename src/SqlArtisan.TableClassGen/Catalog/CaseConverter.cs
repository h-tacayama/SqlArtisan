using System.Text;

namespace SqlArtisan.TableClassGen;

internal static class CaseConverter
{
    // Converts a database name to a PascalCase C# identifier. Splits on any
    // character that cannot appear in an identifier — underscores plus DB-allowed
    // punctuation such as Oracle's '$' and '#' — so those never leak into the
    // result, prefixes an underscore when the name would otherwise start with a
    // digit, and falls back to "_" for a name that is entirely separators.
    public static string SnakeToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
        {
            return snakeCase;
        }

        StringBuilder result = new(snakeCase.Length + 1);
        bool startOfWord = true;

        foreach (char c in snakeCase)
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(startOfWord
                    ? char.ToUpperInvariant(c)
                    : char.ToLowerInvariant(c));
                startOfWord = false;
            }
            else
            {
                startOfWord = true;
            }
        }

        if (result.Length == 0)
        {
            return "_";
        }

        if (char.IsDigit(result[0]))
        {
            result.Insert(0, '_');
        }

        return result.ToString();
    }
}
