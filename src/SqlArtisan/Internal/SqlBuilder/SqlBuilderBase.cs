using System.Runtime.InteropServices;

namespace SqlArtisan.Internal;

internal abstract class SqlBuilderBase
{
    // The collection-expression spread would start the list at the root-part
    // count (usually 1) and immediately reallocate on the first appended clause.
    // Start at the List growth step instead so that initial array is never wasted.
    private const int ExpectedClauseCount = 4;

    private readonly List<SqlPart> _parts;

    // Single-use guard: a successful Build() sets this; afterwards any stage
    // call or Build() throws, blocking silent state contamination from a reused chain.
    private bool _built;

    protected SqlBuilderBase(SqlPart[] rootParts)
    {
        _parts = new List<SqlPart>(Math.Max(rootParts.Length, ExpectedClauseCount));
        _parts.AddRange(rootParts);
    }

    // The SQL spelling of the statement, for the single-use guard message.
    protected abstract string StatementName { get; }

    protected internal void AddPart(SqlPart part)
    {
        ThrowIfBuilt();
        _parts.Add(part);
    }

    protected void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new ArgumentException(
                $"This {StatementName} statement was already built; start a new chain.");
        }
    }

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
        ThrowIfBuilt();
        Validate(dbms);
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);
        using SqlBuildingBuffer buffer = new(dialect);
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));
        AppendTrailing(buffer);
        // Set last so a throw above (Validate / empty-clause guard) leaves the
        // builder usable for a fix-up on the same instance.
        _built = true;
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
    // (currently only an aliased INSERT/UPDATE/DELETE target on SQL Server). The
    // default does nothing, so every other statement builds unchanged. Runs on
    // every build path, since they all funnel through BuildCore (Returning
    // included, via BuildWithPart).
    protected virtual void Validate(Dbms dbms)
    {
    }

    internal void FormatCore(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));
}
