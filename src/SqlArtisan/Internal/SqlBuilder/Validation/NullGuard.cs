namespace SqlArtisan.Internal;

// The required-reference guard for clause-object parameters (a null would
// silently drop the clause the caller wrote); returns the argument so the
// check composes inside an expression-bodied factory.
internal static class NullGuard
{
    internal static T ThrowIfNull<T>(T value, string paramName)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }
}
