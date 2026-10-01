namespace SqlArtisan.Internal;

// INSERT IGNORE and ON DUPLICATE KEY UPDATE are MySQL's alone, and MySQL has no
// RETURNING, so no Build(dbms) target accepts the pairing — OutputClauseGuard's
// no-dialect-accepts-both-halves shape (#400), on the MySQL side.
internal static class ReturningGuard
{
    internal static void ThrowIfCombinedWithMySqlInsertForm(
        InsertIgnoreIntoClause? insertIgnore,
        OnDuplicateKeyUpdateClause? onDuplicateKeyUpdate,
        ReturningClause? returning,
        ReturningIntoClause? returningInto)
    {
        if ((insertIgnore is not null || onDuplicateKeyUpdate is not null)
            && (returning is not null || returningInto is not null))
        {
            throw new ArgumentException(
                "RETURNING cannot be combined with INSERT IGNORE or ON DUPLICATE KEY UPDATE; "
                    + "use one or the other.");
        }
    }

    // The typestate withholds RETURNING on these joined forms, whose only engines
    // (MySQL, SQL Server) have none; this backstops a held stage that reaches both.
    internal static void ThrowIfCombinedWithJoinedDelete(
        DmlJoinState state,
        ReturningClause? returning,
        ReturningIntoClause? returningInto)
    {
        if (state.HasFrom && (returning is not null || returningInto is not null))
        {
            throw new ArgumentException(
                "RETURNING cannot be combined with a joined DELETE ... FROM; "
                    + "join through Using(...) instead.");
        }
    }

    internal static void ThrowIfCombinedWithJoinedUpdate(
        DmlJoinState state,
        ReturningClause? returning,
        ReturningIntoClause? returningInto)
    {
        if ((state.HasDirectJoin || state.TargetRepeatedInFrom)
            && (returning is not null || returningInto is not null))
        {
            throw new ArgumentException(
                "RETURNING cannot be combined with an UPDATE that joins before SET or "
                    + "re-lists its target in FROM; join through From(...) without the "
                    + "target instead.");
        }
    }
}
