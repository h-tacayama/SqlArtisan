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

    // Non-null arms the correlated-DML guard (#253): UPDATE/DELETE return their
    // unaliased target so a target column rendered inside a subquery fails
    // loudly instead of silently resolving to the inner scope.
    private protected virtual DbTableBase? CorrelatedDmlGuardTarget => null;

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
        ThrowIfDuplicateClauseInBlock();
        Validate(dbms);
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);
        using SqlBuildingBuffer buffer = new(dialect);
        buffer.SetCorrelatedDmlGuardTarget(CorrelatedDmlGuardTarget);
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
    // SQL is emitted — the bounded exceptions to ADR 0007 recorded in ADR 0011.
    // The default does nothing, so every other statement builds unchanged. Runs
    // on every build path, since they all funnel through BuildCore (Returning
    // included, via BuildWithPart) — and only for the outermost statement: a
    // nested subquery renders through Format, per ADR 0007's permissive
    // default (RD-002).
    protected virtual void Validate(Dbms dbms)
    {
    }

    internal void FormatCore(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));

    // One entry per clause kind a query block takes at most once; grouped types
    // (the SELECT prefixes, the two OFFSET spellings, WITH vs WITH RECURSIVE,
    // RETURNING vs RETURNING INTO) count as one kind. Clauses that legally
    // repeat — joins, MERGE's WHEN branches, multi-row VALUES — stay out.
    private static readonly (string Name, Type[] Types)[] OncePerBlockClauses =
    [
        ("SELECT", [
            typeof(SelectClause), typeof(SelectClauseWithDistinct), typeof(SelectClauseWithHints),
            typeof(SelectClauseWithOptions), typeof(SelectClauseWithTop),
            typeof(SelectClauseWithDistinctTop),
        ]),
        ("FROM", [typeof(FromClause)]),
        ("WHERE", [typeof(WhereClause)]),
        ("GROUP BY", [typeof(GroupByClause)]),
        ("HAVING", [typeof(HavingClause)]),
        ("ORDER BY", [typeof(OrderByClause)]),
        ("LIMIT", [typeof(LimitClause)]),
        ("OFFSET", [typeof(OffsetClause), typeof(OffsetRowsClause)]),
        ("FETCH", [typeof(FetchClause)]),
        ("FOR UPDATE", [typeof(ForUpdateClause)]),
        ("WITH ROLLUP", [typeof(WithRollupClause)]),
        ("SET", [typeof(UpdateSetClause), typeof(InsertSetClause)]),
        ("USING", [typeof(DeleteUsingClause)]),
        ("ON CONFLICT", [typeof(OnConflictClause)]),
        ("ON DUPLICATE KEY UPDATE", [typeof(OnDuplicateKeyUpdateClause)]),
        ("DO UPDATE SET", [typeof(DoUpdateSetClause)]),
        ("DO NOTHING", [typeof(DoNothingClause)]),
        ("RETURNING", [typeof(ReturningClause), typeof(ReturningIntoClause)]),
        ("OUTPUT", [typeof(OutputClause)]),
        ("OUTPUT INTO", [typeof(OutputIntoClause)]),
        ("WITH", [typeof(WithClause), typeof(WithRecursiveClause)]),
    ];

    // A stage method repeated on a held, not-yet-built builder appends a
    // duplicate clause (`WHERE ... WHERE ...`) — valid on no dialect, so it is
    // rejected here rather than emitted (#225's silent-wrong-SQL class). A set
    // operator starts a new query block, so a compound query's second SELECT
    // legally re-carries every kind.
    private void ThrowIfDuplicateClauseInBlock()
    {
        ulong seen = 0;
        foreach (SqlPart part in CollectionsMarshal.AsSpan(_parts))
        {
            if (part is UnionOperator or ExceptOperator or IntersectOperator or MinusOperator)
            {
                seen = 0;
                continue;
            }

            Type partType = part.GetType();
            for (int i = 0; i < OncePerBlockClauses.Length; i++)
            {
                if (System.Array.IndexOf(OncePerBlockClauses[i].Types, partType) < 0)
                {
                    continue;
                }

                ulong bit = 1UL << i;
                if ((seen & bit) != 0)
                {
                    throw new ArgumentException(
                        $"A statement takes at most one {OncePerBlockClauses[i].Name} clause " +
                        "per query block; a stage on a held builder was called twice.");
                }

                seen |= bit;
                break;
            }
        }
    }

    // The first appended part of type T, or null — for a Validate(Dbms) override
    // to inspect which clauses a chain carries (e.g. a TOP prefix beside OFFSET).
    private protected T? FindPart<T>() where T : class
    {
        foreach (SqlPart part in CollectionsMarshal.AsSpan(_parts))
        {
            if (part is T match)
            {
                return match;
            }
        }

        return null;
    }
}
