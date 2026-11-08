namespace SqlArtisan.Internal;

internal sealed class NamedFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal NamedFunction(string name, SqlExpression[] args)
    {
        StringGuard.ThrowIfNullOrWhiteSpace(name, "A function call requires a function name.");

        // Emitted bare, so the shape keeps out quotes, comments and ')'. ASCII only, since
        // which other characters a name may hold varies by engine; MySQL comments at #.
        if (!IsDottedIdentifier(name))
        {
            throw new ArgumentException(
                "A function name must be ASCII identifiers joined by '.', each starting "
                + "with a letter or _ and holding only letters, digits, _ and $.");
        }

        _core = new(name, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) => _core.Format(buffer);

    private static bool IsDottedIdentifier(string name)
    {
        bool partStart = true;
        foreach (char c in name)
        {
            if (c == '.')
            {
                if (partStart)
                {
                    return false;
                }

                partStart = true;
                continue;
            }

            bool allowed = partStart
                ? char.IsAsciiLetter(c) || c == '_'
                : char.IsAsciiLetterOrDigit(c) || c is '_' or '$';
            if (!allowed)
            {
                return false;
            }

            partStart = false;
        }

        return !partStart;
    }
}
