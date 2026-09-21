namespace SqlArtisan.Internal;

// The Build()-time guard every written condition clause calls from its Format (#236):
// a clause with no runnable condition is rejected, not silently dropped. Format-time,
// not eager, because the clause nodes are shared by every statement type.
internal static class ConditionGuard
{
    internal static void ThrowIfEmpty(SqlPart condition, string message)
    {
        if (condition.IsEmpty)
        {
            throw new ArgumentException(message);
        }
    }
}
