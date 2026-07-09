namespace SqlArtisan.Internal;

// The eager empty-collection guard for the clause factories that require at least
// one item; centralizes the repeated `Length == 0` check, with the
// construct-specific message supplied at the call site.
internal static class CollectionGuard
{
    internal static void ThrowIfEmpty<T>(T[] items, string message)
    {
        if (items.Length == 0)
        {
            throw new ArgumentException(message);
        }
    }
}
