using System.Diagnostics.CodeAnalysis;
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

    // Pre-build hook for ADR 0011's bounded dialect rejections. Every build path
    // funnels through BuildCore, and only the outermost statement runs it — a
    // nested subquery renders through Format, ADR 0007's default (RD-002).
    protected virtual void Validate(Dbms dbms)
    {
    }

    // A nested render (subquery, CTE body, scalar item) never passes through
    // BuildCore, so the dialect-blind walk runs here too: a dangling join or a
    // duplicate clause is no less wrong one level down (release audit, pass 4).
    internal void FormatCore(SqlBuildingBuffer buffer)
    {
        ThrowIfDuplicateClauseInBlock();
        buffer.AppendSpaceSeparated(CollectionsMarshal.AsSpan(_parts));
    }

    // One entry per clause kind a query block takes at most once; grouped
    // types count as one kind, and clauses that legally repeat stay out.
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
        ("USING", [typeof(DeleteUsingClause), typeof(MergeUsingClause)]),
        ("ON", [typeof(MergeOnClause)]),
        ("ON CONFLICT", [typeof(OnConflictClause)]),
        ("ON DUPLICATE KEY UPDATE", [typeof(OnDuplicateKeyUpdateClause)]),
        ("DO UPDATE SET", [typeof(DoUpdateSetClause)]),
        ("DO NOTHING", [typeof(DoNothingClause)]),
        ("RETURNING", [typeof(ReturningClause), typeof(ReturningIntoClause)]),
        ("OUTPUT", [typeof(OutputClause)]),
        ("OUTPUT INTO", [typeof(OutputIntoClause)]),
        ("WITH", [typeof(WithClause), typeof(WithRecursiveClause)]),
    ];

    // INSERT ... WITH ... SELECT: the feeding SELECT is its own query block for
    // WITH, so a leading With(...) and the mid-chain With(...) legally coexist.
    private static readonly int WithClauseIndex =
        System.Array.FindIndex(OncePerBlockClauses, entry => entry.Name == "WITH");

    // MERGE's per-branch action kinds: at most one per WHEN branch, where a new
    // WHEN clause opens a fresh branch (so a legal multi-branch MERGE re-carries
    // them). InsertValuesClause is safe here for plain INSERT too — its Values
    // overloads grow one held clause, never a second part.
    private static readonly (string Name, Type[] Types)[] OncePerBranchClauses =
    [
        ("UPDATE SET", [typeof(MergeUpdateSetClause)]),
        ("DELETE", [typeof(MergeDeleteClause)]),
        ("DELETE WHERE", [typeof(MergeDeleteWhereClause)]),
        ("INSERT", [typeof(MergeInsertClause)]),
        ("VALUES", [typeof(InsertValuesClause)]),
    ];

    // A stage repeated on a held, not-yet-built builder appends a duplicate
    // clause — valid on no dialect (#225's silent-wrong-SQL class). A set
    // operator opens a new block, a MERGE WHEN a new branch, and each join
    // re-admits one ON/USING — which a conditioned join must also *receive*
    // before anything else follows (ADR 0017's Build()-time backstop).
    private void ThrowIfDuplicateClauseInBlock()
    {
        ulong seen = 0;
        ulong seenInBranch = 0;
        bool joinConditionSeen = false;
        bool joinConditionPending = false;
        foreach (SqlPart part in CollectionsMarshal.AsSpan(_parts))
        {
            if (joinConditionPending && part is not (OnClause or JoinUsingClause))
            {
                ThrowJoinConditionMissing();
            }

            if (part is UnionOperator or ExceptOperator or IntersectOperator or MinusOperator)
            {
                seen = 0;
                continue;
            }

            if (part is WhenMatchedClause or WhenNotMatchedClause or WhenNotMatchedBySourceClause)
            {
                seenInBranch = 0;
                continue;
            }

            if (part is InsertIntoClause or InsertIgnoreIntoClause)
            {
                seen &= ~(1UL << WithClauseIndex);
            }

            // ON/USING legally repeat once per join, so they pair by adjacency
            // rather than by a once-per-block entry.
            if (part is InnerJoinClause or LeftJoinClause or RightJoinClause or FullJoinClause
                or JoinLateralClause)
            {
                joinConditionSeen = false;
                joinConditionPending = true;
            }
            else if (part is LeftJoinLateralClause or CrossJoinLateralClause
                or CrossJoinClause or NaturalJoinClause or NaturalLeftJoinClause
                or NaturalRightJoinClause or NaturalFullJoinClause
                or CrossApplyClause or OuterApplyClause)
            {
                joinConditionSeen = false;
            }
            else if (part is OnClause or JoinUsingClause)
            {
                if (joinConditionSeen)
                {
                    throw new ArgumentException(
                        "A join takes at most one ON or USING clause; "
                        + "a stage on a held builder was called twice.");
                }

                joinConditionSeen = true;
                joinConditionPending = false;
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
                        $"A statement takes at most one {OncePerBlockClauses[i].Name} clause "
                        + "per query block; a stage on a held builder was called twice.");
                }

                seen |= bit;
                break;
            }

            for (int i = 0; i < OncePerBranchClauses.Length; i++)
            {
                if (System.Array.IndexOf(OncePerBranchClauses[i].Types, partType) < 0)
                {
                    continue;
                }

                ulong bit = 1UL << i;
                if ((seenInBranch & bit) != 0)
                {
                    throw new ArgumentException(
                        $"A MERGE WHEN branch takes at most one {OncePerBranchClauses[i].Name} "
                        + "clause; a stage on a held builder was called twice.");
                }

                seenInBranch |= bit;
                break;
            }
        }

        if (joinConditionPending)
        {
            ThrowJoinConditionMissing();
        }
    }

    [DoesNotReturn]
    private static void ThrowJoinConditionMissing() =>
        throw new ArgumentException(
            "A join is missing its ON or USING clause; the statement was built "
            + "from a held builder before the join was completed.");

    // For a Validate(Dbms) override that must walk clause order (e.g. MERGE's
    // branch pairing), where FindPart's first-of-type is not enough.
    private protected ReadOnlySpan<SqlPart> PartsSpan => CollectionsMarshal.AsSpan(_parts);

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
