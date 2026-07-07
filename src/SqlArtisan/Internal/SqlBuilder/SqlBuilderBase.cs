using System.Runtime.InteropServices;

namespace SqlArtisan.Internal;

internal abstract class SqlBuilderBase
{
    // The collection-expression spread would start the list at the root-part
    // count (usually 1) and immediately reallocate on the first appended clause.
    // Start at the List growth step instead so that initial array is never wasted.
    private const int ExpectedClauseCount = 4;

    private readonly List<SqlPart> _parts;

    protected SqlBuilderBase(SqlPart[] rootParts)
    {
        _parts = new List<SqlPart>(Math.Max(rootParts.Length, ExpectedClauseCount));
        _parts.AddRange(rootParts);
    }

    protected internal void AddPart(SqlPart part) => _parts.Add(part);

    internal SqlStatement BuildWithPart(SqlPart extraPart, Dbms dbms)
    {
        _parts.Add(extraPart);
        try
        {
            return BuildCore(dbms);
        }
        finally
        {
            _parts.RemoveAt(_parts.Count - 1);
        }
    }

    internal SqlStatement BuildWithPart(SqlPart extraPart) =>
        BuildWithPart(extraPart, SqlArtisanConfig.DefaultDbms);

    protected SqlStatement BuildCore(Dbms dbms)
    {
        Preflight(dbms);
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);
        using SqlBuildingBuffer buffer = new(dialect);
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));
        AppendTrailing(buffer);
        return buffer.ToSqlStatement();
    }

    // Hook for statements that need a trailing token after all clauses (e.g. the
    // SQL Server MERGE terminating semicolon). The default emits nothing, leaving
    // every other statement's output untouched.
    protected virtual void AppendTrailing(SqlBuildingBuffer buffer)
    {
    }

    // Pre-build check: a statement builder overrides this to reject an
    // otherwise-grammatical construct for a specific target dialect before any
    // SQL is emitted — the bounded exception to ADR 0007 recorded in ADR 0011
    // (currently only an aliased UPDATE/DELETE target on SQL Server). The default
    // does nothing, so every other statement builds unchanged. Runs on every
    // build path, since they all funnel through BuildCore (Returning included,
    // via BuildWithPart).
    protected virtual void Preflight(Dbms dbms)
    {
    }

    internal void FormatCore(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));
}
