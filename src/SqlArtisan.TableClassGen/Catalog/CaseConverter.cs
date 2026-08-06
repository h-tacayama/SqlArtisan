using System.Text;

namespace SqlArtisan.TableClassGen;

internal static class CaseConverter
{
    // Splits on every non-alphanumeric character — underscores plus DB-allowed
    // punctuation such as Oracle's '$' and '#' — so no punctuation can leak into
    // the emitted identifier.
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
