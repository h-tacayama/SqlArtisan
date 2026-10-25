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

    // RETURNING ... INTO is Oracle's, which has no ON CONFLICT; the engines with
    // ON CONFLICT take RETURNING without INTO.
    internal static void ThrowIfIntoCombinedWithOnConflict(
        OnConflictClause? onConflict, ReturningIntoClause? returningInto)
    {
        if (onConflict is not null && returningInto is not null)
        {
            throw new ArgumentException(
                "RETURNING ... INTO cannot be combined with ON CONFLICT; "
                    + "build the RETURNING stage without Into(...) instead.");
        }
    }

    // Oracle, the one engine with RETURNING ... INTO, takes no leading WITH on a DML
    // statement (ThrowIfLeadingWithUnsupported), so no target runs the pair.
    internal static void ThrowIfIntoAfterLeadingWith(
        ReadOnlySpan<SqlPart> parts, ReturningIntoClause? returningInto)
    {
        if (returningInto is not null && DmlTargetGuard.LeadsWithCte(parts))
        {
            throw new ArgumentException(
                "RETURNING ... INTO cannot be combined with a leading WITH; "
                    + "inline the subquery instead.");
        }
    }

    // The typestate withholds RETURNING on these joined forms, which MySQL and SQL Server
    // spell without one and Oracle 23.26.3 rejects re-listed; this backstops a held stage.
    internal static void ThrowIfCombinedWithJoinedDelete(
        DmlJoinState state,
        ReturningClause? returning,
        ReturningIntoClause? returningInto)
    {
        if (state.TargetRepeatedInFrom && (returning is not null || returningInto is not null))
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
