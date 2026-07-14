namespace SqlArtisan.Internal;

// The eager empty-collection guard for the clause factories that require at least
// one item; centralizes the repeated `Length == 0` check, with the
// construct-specific message supplied at the call site.
internal static class CollectionGuard
{
    internal static void ThrowIfEmpty<T>(T[] items, string message)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Length == 0)
        {
            throw new ArgumentException(message);
        }
    }

    internal static void ThrowIfEmpty<T>(IReadOnlyCollection<T> items, string message)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException(message);
        }
    }
}
