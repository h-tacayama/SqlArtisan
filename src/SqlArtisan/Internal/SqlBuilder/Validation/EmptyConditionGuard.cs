namespace SqlArtisan.Internal;

// The Build()-time guard for constructs where an all-empty condition is a bug,
// not "no restriction" (the #236 empty-state policy): a JOIN/MERGE ON, a CASE
// WHEN, an UPDATE/DELETE WHERE, a MERGE WHEN MATCHED AND / DELETE WHERE. Eliding
// there would silently emit invalid SQL or, on DML, widen the blast radius to a
// full-table write — so these fail loudly instead. SELECT WHERE/HAVING and the
// aggregate FILTER elide and never reach here. The check runs at Build()/format
// time, not eagerly, because `operator &` mutates a held AND group: a tree that
// is empty when the clause method is called can legitimately become non-empty
// before Build().
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
