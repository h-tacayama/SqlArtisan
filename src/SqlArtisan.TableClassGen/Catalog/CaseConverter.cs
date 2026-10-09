using System.Globalization;
using System.Text;

namespace SqlArtisan.TableClassGen;

internal static class CaseConverter
{
    // Splits on the underscore and on every character C# rejects in an identifier (Oracle's
    // '$'/'#'), so none leaks into it; a mixed-case run keeps its casing past the first letter.
    public static string SnakeToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
        {
            return snakeCase;
        }

        // C# ignores formatting characters when comparing names, so keeping one would let two
        // names the compiler calls equal past the ordinal collision guards.
        string name = RemoveFormattingCharacters(snakeCase);
        StringBuilder result = new(name.Length + 1);
        int runStart = 0;

        for (int i = 0; i <= name.Length; i++)
        {
            if (i == name.Length || !IsIdentifierPart(name[i]))
            {
                AppendRun(result, name.AsSpan(runStart, i - runStart));
                runStart = i + 1;
            }
        }

        if (result.Length == 0)
        {
            return "_";
        }

        if (!IsIdentifierStart(result[0]))
        {
            result.Insert(0, '_');
        }

        return result.ToString();
    }

    // Read per UTF-16 unit, never per Rune: C# admits no character outside the BMP (CS1056),
    // so a surrogate separates.
    private static bool IsIdentifierPart(char c) =>
        c != '_'
        && (IsIdentifierStart(c)
            || char.GetUnicodeCategory(c) is UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.ConnectorPunctuation);

    private static bool IsIdentifierStart(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.LetterNumber;

    private static string RemoveFormattingCharacters(string name)
    {
        StringBuilder kept = new(name.Length);
        foreach (char c in name)
        {
            if (char.GetUnicodeCategory(c) != UnicodeCategory.Format)
            {
                kept.Append(c);
            }
        }

        return kept.ToString();
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
