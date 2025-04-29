namespace InlineSqlSharp.TableClassGen;

internal static class CaseConverter
{
    public static string SnakeToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
        {
            return snakeCase;
        }

        string[] parts = snakeCase.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(
            parts.Select(
                part => $"{char.ToUpperInvariant(part[0])}{part.Substring(1).ToLowerInvariant()}"));
    }
}
