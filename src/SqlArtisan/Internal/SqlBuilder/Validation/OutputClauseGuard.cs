namespace SqlArtisan.Internal;

// Every guarded pair here is one SQL-Server-only construct beside one
// SQL-Server-incompatible one, so no Build(dbms) target ever accepts both --
// the same shape as SelectBuilder's TOP + OFFSET/FETCH exclusivity check.
internal static class OutputClauseGuard
{
    internal static void ThrowIfCombinedWithReturning(
        OutputClause? output, ReturningClause? returning, ReturningIntoClause? returningInto)
    {
        if (output is not null && (returning is not null || returningInto is not null))
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with RETURNING; use one or the other.");
        }
    }

    internal static void ThrowIfDeleteCombinedWithUsing(
        OutputClause? output, DeleteUsingClause? using_)
    {
        if (output is not null && using_ is not null)
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with USING; use one or the other.");
        }
    }

    internal static void ThrowIfInsertCombinedWithUpsert(
        OutputClause? output,
        OnConflictClause? onConflict,
        OnDuplicateKeyUpdateClause? onDuplicateKeyUpdate)
    {
        if (output is not null && (onConflict is not null || onDuplicateKeyUpdate is not null))
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with ON CONFLICT or ON DUPLICATE KEY UPDATE; "
                    + "use one or the other.");
        }
    }
}
