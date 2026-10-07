namespace SqlArtisan.TableClassGen;

internal static class DefaultExpression
{
    // An explicit DEFAULT NULL is recorded like any default, yet supplies nothing:
    // read as one, it silenced SQLA0202 on a NOT NULL column the INSERT must fill.
    // SQL Server stores it as `(NULL)`, PostgreSQL as `NULL::character varying`.
    public static bool IsNull(string text)
    {
        string expression = text.Trim();
        int cast = expression.IndexOf("::", StringComparison.Ordinal);

        if (cast >= 0)
        {
            expression = expression[..cast].Trim();
        }

        while (expression.Length >= 2 && expression[0] == '(' && expression[^1] == ')')
        {
            expression = expression[1..^1].Trim();
        }

        return string.Equals(expression, "NULL", StringComparison.OrdinalIgnoreCase);
    }
}
