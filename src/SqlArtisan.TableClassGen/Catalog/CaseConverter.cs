using System.Text;

namespace SqlArtisan.TableClassGen;

internal static class CaseConverter
{
    // Splits on every non-alphanumeric character (underscores, Oracle's '$'/'#') so
    // none leaks into the identifier; a mixed-case run keeps its casing past the first letter.
    public static string SnakeToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
        {
            return snakeCase;
        }

        StringBuilder result = new(snakeCase.Length + 1);
        int runStart = 0;

        for (int i = 0; i <= snakeCase.Length; i++)
        {
            if (i == snakeCase.Length || !char.IsLetterOrDigit(snakeCase[i]))
            {
                AppendRun(result, snakeCase.AsSpan(runStart, i - runStart));
                runStart = i + 1;
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

    private static void AppendRun(StringBuilder result, ReadOnlySpan<char> run)
    {
        if (run.Length == 0)
        {
            return;
        }

        bool upper = false;
        bool lower = false;
        foreach (char c in run)
        {
            upper |= char.IsUpper(c);
            lower |= char.IsLower(c);
        }

        result.Append(char.ToUpperInvariant(run[0]));
        ReadOnlySpan<char> rest = run[1..];
        if (upper && lower)
        {
            result.Append(rest);
        }
        else
        {
            foreach (char c in rest)
            {
                result.Append(char.ToLowerInvariant(c));
            }
        }
    }
}
