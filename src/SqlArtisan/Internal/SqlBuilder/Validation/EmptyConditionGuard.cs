namespace SqlArtisan.Internal;

// The Build()-time guard every written condition clause calls from its Format
// (the #236 empty-state policy): WHERE (SELECT/UPDATE/DELETE), HAVING, aggregate
// FILTER, JOIN/MERGE ON, CASE WHEN, and MERGE WHEN [NOT] MATCHED / DELETE WHERE.
// A clause with no runnable condition is rejected rather than silently dropped —
// "no restriction" is spelled by omitting the clause. The check runs at
// Build()/format time, not eagerly, because `operator &` mutates a held AND
// group: a tree that is empty when the clause method is called can legitimately
// become non-empty before Build().
internal static class EmptyConditionGuard
{
    internal static void Reject(SqlPart condition, string message)
    {
        if (condition.IsEmpty)
        {
            throw new ArgumentException(message);
        }
    }
}
