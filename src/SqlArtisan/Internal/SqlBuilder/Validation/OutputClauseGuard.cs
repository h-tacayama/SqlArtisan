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

    // SQL Server, the one engine with OUTPUT, takes a WITH only ahead of INSERT INTO.
    internal static void ThrowIfInsertCombinedWithSelectWith(
        OutputClause? output, ReadOnlySpan<SqlPart> parts)
    {
        if (output is null)
        {
            return;
        }

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i] is WithClause or WithRecursiveClause)
            {
                throw new ArgumentException(
                    "OUTPUT cannot be combined with a WITH between INSERT INTO and its "
                        + "SELECT; put the WITH ahead of INSERT INTO instead.");
            }
        }
    }

    // The direct join is MySQL's, which has no OUTPUT; the typestate withholds
    // Output after one, so this backstops a held stage that reaches both.
    internal static void ThrowIfUpdateCombinedWithDirectJoin(
        OutputClause? output, DmlJoinState state)
    {
        if (output is not null && state.HasDirectJoin)
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with an UPDATE that joins before SET; "
                    + "join through From(...), re-listing the target, instead.");
        }
    }

    // SQL Server, the one engine with OUTPUT, spells a joined UPDATE only by re-listing
    // its target in FROM (ThrowIfSqlServerJoinedTargetNotRepeated).
    internal static void ThrowIfUpdateCombinedWithUnrelistedFrom(
        OutputClause? output, DmlJoinState state)
    {
        if (output is not null && state.HasFrom && !state.TargetRepeatedInFrom)
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with an UPDATE ... FROM that does not re-list its "
                    + "target; re-list the target in From(...) instead.");
        }
    }

    // #397's width class for the OUTPUT ... INTO redirect: an explicit INTO
    // column list must match the OUTPUT list one-to-one; a star item's width is
    // the schema's, so it is left to the engine.
    internal static void ThrowIfIntoWidthMismatch(OutputClause? output, OutputIntoClause? into)
    {
        if (output is null || into is null || into.ColumnCount == 0)
        {
            return;
        }

        foreach (SqlPart item in output.Items)
        {
            if (item is AsteriskMarker or QualifiedAsteriskMarker)
            {
                return;
            }
        }

        if (output.Items.Length != into.ColumnCount)
        {
            throw new ArgumentException(
                $"The OUTPUT list has {output.Items.Length} item(s), "
                    + $"but the INTO column list declares {into.ColumnCount} column(s).");
        }
    }
}
