namespace SqlArtisan.Internal;

// The required-string guard for node constructors whose SQL is invalid or
// nonsensical when a mandatory token is missing; the construct-specific message
// is supplied at the call site.
internal static class StringGuard
{
    internal static void ThrowIfNullOrEmpty(string value, string message)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(message);
        }
    }

    // For a bare-token position (a CAST type, a NEXT VALUE FOR sequence name),
    // where whitespace is invalid on every dialect — unlike a quoted one.
    internal static void ThrowIfNullOrWhiteSpace(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }
    }
}
