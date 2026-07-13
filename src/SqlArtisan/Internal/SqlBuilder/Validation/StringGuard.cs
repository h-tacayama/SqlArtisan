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
}
