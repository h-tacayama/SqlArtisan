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

    // Non-null arms the correlated-DML guard (#253): UPDATE/DELETE/MERGE return
    // their unaliased target so a target column rendered inside a subquery fails
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

    // Returning(...) hands the chain to a stage that appends its clause only when it
    // builds, so a build that does not carry the clause would drop it silently; every
    // live stage is counted, and a build carrying none of them throws in the walk.
    private int _pendingReturning;

    internal void MarkReturningPending()
    {
        ThrowIfBuilt();
        _pendingReturning++;
    }

    internal void DischargeReturning() => _pendingReturning--;

    internal SqlStatement BuildWithPart(SqlPart extraPart, Dbms dbms)
    {
        ThrowIfBuilt();
        if (_pendingReturning > 1)
        {
            throw new ArgumentException(
                "Two RETURNING stages were written on this statement; a build from either "
                + "would drop the other, so write RETURNING once.");
        }

        // The stage carries its own clause; a failed build restores the obligation
        // with the parts, so a retry from the earlier stage still throws.
        int pending = _pendingReturning;
        _parts.Add(extraPart);
        _pendingReturning = 0;
        try
        {
            return BuildCore(dbms);
        }
        finally
        {
            _parts.RemoveAt(_parts.Count - 1);
            _pendingReturning = pending;
        }
    }

    internal SqlStatement BuildWithPart(SqlPart extraPart) =>
        BuildWithPart(extraPart, SqlArtisanConfig.DefaultDbms);

    protected SqlStatement BuildCore(Dbms dbms)
    {
        ThrowIfBuilt();
        ThrowIfDuplicateClauseInBlock();
        Validate(dbms);
        using SqlBuildingBuffer buffer = new(dbms);
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

    // Pre-build hook for ADR 0011's bounded dialect rejections, run once per
    // query block against the target the outermost Build(Dbms) resolved.
    protected virtual void Validate(Dbms dbms)
    {
    }

    // A nested render (subquery, CTE body, scalar item) never passes through
    // BuildCore, so both pre-build checks run here too — a duplicate clause or a
    // rejected ordinal is no less wrong one level down (release audit, pass 4).
    internal void FormatCore(SqlBuildingBuffer buffer)
    {
        ThrowIfDuplicateClauseInBlock();
        Validate(buffer.Dbms);
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
    private static readonly int WithClauseIndex = IndexOfKind("WITH");

    // Spellings of one slot a block takes one of (LIMIT beside FETCH, say): distinct
    // kinds the once-per-kind check cannot see, so they pair here.
    private static readonly (int Kind, int Other)[] ExclusiveKinds = BuildExclusiveKinds(
        OncePerBlockClauses,
        ["LIMIT", "FETCH"],
        ["FROM", "USING"],
        ["ON CONFLICT", "ON DUPLICATE KEY UPDATE"],
        ["DO NOTHING", "DO UPDATE SET"]);

    private static int IndexOfKind(string name) =>
        IndexOfKind(OncePerBlockClauses, name);

    private static int IndexOfKind((string Name, Type[] Types)[] kinds, string name) =>
        System.Array.FindIndex(kinds, entry => entry.Name == name);

    // Every ordered pair across each group, so the check reads one table.
    private static (int Kind, int Other)[] BuildExclusiveKinds(
        (string Name, Type[] Types)[] kinds, params string[][] groups)
    {
        List<(int, int)> pairs = [];
        foreach (string[] group in groups)
        {
            for (int i = 0; i < group.Length; i++)
            {
                for (int j = 0; j < group.Length; j++)
                {
                    if (i != j)
                    {
                        pairs.Add((IndexOfKind(kinds, group[i]), IndexOfKind(kinds, group[j])));
                    }
                }
            }
        }

        return [.. pairs];
    }

    // MERGE's per-branch action kinds: at most one per WHEN branch, each WHEN
    // opening a fresh branch. InsertValuesClause is safe for plain INSERT too — its
    // Values overloads grow one held clause, never a second part.
    private static readonly (string Name, Type[] Types)[] OncePerBranchClauses =
    [
        ("UPDATE SET", [typeof(MergeUpdateSetClause)]),
        ("DELETE", [typeof(MergeDeleteClause)]),
        ("DELETE WHERE", [typeof(MergeDeleteWhereClause)]),
        ("INSERT", [typeof(MergeInsertClause)]),
        ("VALUES", [typeof(InsertValuesClause)]),
    ];

    // A branch takes one action; only Oracle's `UPDATE SET ... DELETE WHERE`
    // pairs two, so DELETE WHERE is exclusive with the other two alone.
    private static readonly (int Kind, int Other)[] ExclusiveBranchKinds = BuildExclusiveKinds(
        OncePerBranchClauses,
        ["UPDATE SET", "DELETE", "INSERT"],
        ["DELETE WHERE", "DELETE"],
        ["DELETE WHERE", "INSERT"]);

    // A stage repeated on a held builder appends a duplicate clause, valid on no
    // dialect (#225); a set operator and a conditioned join each open what they must
    // also receive (ADR 0017; a MERGE WHEN's action pairing is MergeBuilder.Validate's).
    private void ThrowIfDuplicateClauseInBlock()
    {
        ulong seen = 0;
        ulong seenInBranch = 0;
        bool joinConditionSeen = false;
        bool joinConditionPending = false;
        bool setOperatorPending = false;
        bool inInsert = false;
        bool insertSourceSeen = false;
        foreach (SqlPart part in CollectionsMarshal.AsSpan(_parts))
        {
            if (joinConditionPending && part is not (OnClause or JoinUsingClause))
            {
                ThrowJoinConditionMissing();
            }

            if (setOperatorPending && part is not ISelectItemsClause)
            {
                ThrowSetOperatorOperandMissing();
            }

            if (part is UnionOperator or ExceptOperator or IntersectOperator or MinusOperator)
            {
                seen = 0;
                setOperatorPending = true;
                // The feeding SELECT's next branch is its own row source.
                insertSourceSeen = false;
                continue;
            }

            setOperatorPending = false;

            if (part is WhenMatchedClause or WhenNotMatchedClause or WhenNotMatchedBySourceClause)
            {
                seenInBranch = 0;
                continue;
            }

            if (part is InsertIntoClause or InsertIgnoreIntoClause)
            {
                seen &= ~(1UL << WithClauseIndex);
                inInsert = true;
            }

            // VALUES, SET, and SELECT spell an INSERT's one row source (SET renders
            // as VALUES), a duplicate the kind-level checks below cannot see.
            if (inInsert && part is InsertValuesClause or InsertSetClause or ISelectItemsClause)
            {
                if (insertSourceSeen)
                {
                    throw new ArgumentException(
                        "An INSERT takes one row source — VALUES, SET, or SELECT; "
                        + "a stage on a held builder supplied a second.");
                }

                insertSourceSeen = true;
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
                // Consumes the slot: an ON after a condition-free join is a repeated
                // stage, not its condition (`CROSS JOIN ... ON` runs on SQLite, wrong).
                joinConditionSeen = true;
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

                for (int j = 0; j < ExclusiveKinds.Length; j++)
                {
                    if (ExclusiveKinds[j].Kind == i
                        && (seen & (1UL << ExclusiveKinds[j].Other)) != 0)
                    {
                        throw new ArgumentException(
                            $"{OncePerBlockClauses[i].Name} cannot be combined with "
                            + $"{OncePerBlockClauses[ExclusiveKinds[j].Other].Name} in one query "
                            + "block; a stage on a held builder supplied both.");
                    }
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

                for (int j = 0; j < ExclusiveBranchKinds.Length; j++)
                {
                    if (ExclusiveBranchKinds[j].Kind == i
                        && (seenInBranch & (1UL << ExclusiveBranchKinds[j].Other)) != 0)
                    {
                        string other = OncePerBranchClauses[ExclusiveBranchKinds[j].Other].Name;
                        throw new ArgumentException(
                            $"A MERGE WHEN branch takes one action; {OncePerBranchClauses[i].Name} "
                            + $"cannot be combined with {other}, "
                            + "and a stage on a held builder supplied both.");
                    }
                }

                seenInBranch |= bit;
                break;
            }
        }

        if (joinConditionPending)
        {
            ThrowJoinConditionMissing();
        }

        if (setOperatorPending)
        {
            ThrowSetOperatorOperandMissing();
        }

        if (_pendingReturning > 0)
        {
            throw new ArgumentException(
                "A RETURNING clause was written on this statement, but it was built from "
                + "the stage before it; build from the RETURNING stage.");
        }
    }

    [DoesNotReturn]
    private static void ThrowJoinConditionMissing() =>
        throw new ArgumentException(
            "A join is missing its ON or USING clause; the statement was built "
            + "from a held builder before the join was completed.");

    [DoesNotReturn]
    private static void ThrowSetOperatorOperandMissing() =>
        throw new ArgumentException(
            "A set operator is missing its SELECT; the statement was built "
            + "from a held builder before the operator was completed.");

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
