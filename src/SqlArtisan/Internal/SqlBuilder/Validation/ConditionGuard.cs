namespace SqlArtisan.Internal;

// The Build()-time guard every written condition clause calls from its Format
// (the #236 empty-state policy): a clause with no runnable condition is rejected
// rather than silently dropped — "no restriction" means omitting the clause.
// Checked at Build(), not eagerly, because `operator &` can make a held AND group
// non-empty after the clause method returns.
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
