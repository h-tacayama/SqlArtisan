namespace SqlArtisan.Internal;

// No Build(dbms) target accepts either half's pairing, so every guard here is
// dialect-blind: three pair OUTPUT with a construct SQL Server does not have,
// and one pairs it with a clause T-SQL takes only ahead of OUTPUT.
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

    // T-SQL puts an INSERT's column list ahead of OUTPUT, and Set(...) is what
    // emits that list, so the pair is invalid in either order — the typestate
    // withholds it, and a held builder can still append both (#521).
    internal static void ThrowIfInsertCombinedWithSet(
        OutputClause? output, InsertSetClause? set)
    {
        if (output is not null && set is not null)
        {
            throw new ArgumentException(
                "OUTPUT cannot be combined with Set(...); name the columns with "
                    + "InsertInto(table, columns) and supply the row with Values(...).");
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
