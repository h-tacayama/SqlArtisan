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

    // The element-level companion: a computed null element reaches the call with
    // no compiler warning, so it gets a named exception rather than a later NRE
    // (guards-and-empty-states rule, element clause). paramName is the caller's
    // parameter so the failure surface never names an internal one.
    internal static void ThrowIfNullElement<T>(T[] items, string paramName, string message)
        where T : class
    {
        // A null array is the C# binding for a trailing null argument
        // (Using(col, null)), so it gets the same construct-named message.
        if (items is null)
        {
            throw new ArgumentNullException(paramName, message);
        }

        foreach (T item in items)
        {
            if (item is null)
            {
                throw new ArgumentNullException(paramName, message);
            }
        }
    }
}
