namespace SqlArtisan.Internal;

// The eager guard for the many clause factories that require at least one item
// (SELECT/GROUP BY/ORDER BY/PARTITION BY/DISTINCT ON lists, RETURNING and its
// INTO outputs). Each formerly repeated the same `Length == 0` check inline; this
// centralizes the throw so the shape is written once. The construct-specific
// message stays at the call site — the wording names that construct and is
// asserted verbatim by its tests.
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
