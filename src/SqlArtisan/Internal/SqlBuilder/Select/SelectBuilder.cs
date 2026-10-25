using System.Diagnostics;

namespace SqlArtisan.Internal;

internal class SelectBuilder :
    SqlBuilderBase,
    ISelectBuilderForUpdate,
    ISelectBuilderFrom,
    ISelectBuilderGroupBy,
    ISelectBuilderHaving,
    ISelectBuilderJoin,
    ISelectBuilderLimitOffset,
    ISelectBuilderOffsetFetch,
    ISelectBuilderOrderBy,
    ISelectBuilderPaginated,
    ISelectBuilderSelect,
    ISelectBuilderSetOperator,
    ISelectBuilderWhere,
    ISelectBuilderWithRollup
{
    internal SelectBuilder(params SqlPart[] rootParts) : base(rootParts)
    {
    }

    private protected SelectBuilder(SelectBuilder leftBranch) : base(leftBranch)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Except => ContinueAfter(new ExceptOperator(false));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator ExceptAll => ContinueAfter(new ExceptOperator(true));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Intersect => ContinueAfter(new IntersectOperator(false));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator IntersectAll => ContinueAfter(new IntersectOperator(true));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Minus => ContinueAfter(new MinusOperator(false));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MinusAll => ContinueAfter(new MinusOperator(true));

    protected override string StatementName => Keywords.Select;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Union => ContinueAfter(new UnionOperator(false));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UnionAll => ContinueAfter(new UnionOperator(true));

    // A held left branch would grow into the right one, so the compound continues
    // on a new builder and the left is frozen (#676).
    private SelectBuilder ContinueAfter(SqlPart setOperator)
    {
        ThrowIfBuilt();
        SelectBuilder next = CreateContinuation();
        FreezeAsContinued();
        next.AddPart(setOperator);
        return next;
    }

    private protected virtual SelectBuilder CreateContinuation() => new(this);

    // Whether a FROM or JOIN of this query lists the relation itself; a compound's
    // branches count together, so a branch's capture is missed, never invented.
    internal bool ListsRelation(TableReference relation)
    {
        foreach (SqlPart part in PartsSpan)
        {
            if (part is FromClause from)
            {
                foreach (TableReference table in from.Tables)
                {
                    if (ReferenceEquals(table, relation))
                    {
                        return true;
                    }
                }
            }
            else if (part is IJoinedRelation join && ReferenceEquals(join.Relation, relation))
            {
                return true;
            }
        }

        return false;
    }

    // Whether any FROM or JOIN relation of this subquery's blocks exposes the
    // RETURNING target's name or alias (SqlBuildingBuffer, #595).
    internal bool BindsAnyRelation(DbTableBase target)
    {
        foreach (SqlPart part in PartsSpan)
        {
            if (part is FromClause from)
            {
                foreach (TableReference table in from.Tables)
                {
                    if (SqlBuildingBuffer.BindsName(table, target))
                    {
                        return true;
                    }
                }
            }
            else if (part is IJoinedRelation join
                && SqlBuildingBuffer.BindsName(join.Relation, target))
            {
                return true;
            }
        }

        return false;
    }

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public ISelectBuilderFrom CrossApply(ISubquery subquery, DerivedTableBase alias)
    {
        AddPart(new CrossApplyClause(subquery, alias));
        return this;
    }

    public ISelectBuilderFrom CrossJoin(TableReference table)
    {
        AddPart(new CrossJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, DerivedTableBase alias)
    {
        AddPart(new CrossJoinLateralClause(subquery, alias));
        return this;
    }

    public ISelectBuilderPaginated FetchFirst(int count)
    {
        AddPart(new FetchClause(count, first: true));
        return this;
    }

    public ISelectBuilderPaginated FetchNext(int count)
    {
        AddPart(new FetchClause(count, first: false));
        return this;
    }

    // The first SELECT clause is the compound's anchor; its resolved items are
    // fixed at creation, so a later set-operator branch cannot change them.
    internal SqlPart[]? FirstSelectItems() => FindPart<ISelectItemsClause>()?.SelectItems;

    public void Format(SqlBuildingBuffer buffer) => FormatCore(buffer);

    public ISelectBuilderForUpdate ForUpdate(LockBehaviorBase? lockBehavior = null)
    {
        AddPart(new ForUpdateClause(lockBehavior));
        return this;
    }

    public ISelectBuilderForUpdate ForUpdate(
        OfClause ofClause,
        LockBehaviorBase? lockBehavior = null)
    {
        // A null here would silently drop the OF list and widen the lock to every
        // table; the lockBehavior-only overload spells that on purpose.
        AddPart(new ForUpdateClause(
            NullGuard.ThrowIfNull(ofClause, nameof(ofClause)),
            lockBehavior));
        return this;
    }

    public ISelectBuilderFrom From(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(
            tables, nameof(tables), "FROM requires at least one table.",
            FromClause.NullTableMessage);
        AddPart(new FromClause(tables));
        return this;
    }

    public ISelectBuilderJoin FullJoin(TableReference table)
    {
        AddPart(new FullJoinClause(table));
        return this;
    }

    public ISelectBuilderGroupBy GroupBy(params object[] groupByItems)
    {
        AddPart(GroupByClause.Parse(groupByItems));
        return this;
    }

    public ISelectBuilderHaving Having(SqlCondition condition)
    {
        AddPart(new HavingClause(condition));
        return this;
    }

    public ISelectBuilderJoin InnerJoin(TableReference table)
    {
        AddPart(new InnerJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin JoinLateral(ISubquery subquery, DerivedTableBase alias)
    {
        AddPart(new JoinLateralClause(subquery, alias));
        return this;
    }

    public ISelectBuilderJoin LeftJoin(TableReference table)
    {
        AddPart(new LeftJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, DerivedTableBase alias)
    {
        AddPart(new LeftJoinLateralClause(subquery, alias));
        return this;
    }

    public ISelectBuilderLimitOffset Limit(int count)
    {
        AddPart(new LimitClause(count));
        return this;
    }

    public ISelectBuilderFrom NaturalFullJoin(TableReference table)
    {
        AddPart(new NaturalFullJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom NaturalJoin(TableReference table)
    {
        AddPart(new NaturalJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom NaturalLeftJoin(TableReference table)
    {
        AddPart(new NaturalLeftJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom NaturalRightJoin(TableReference table)
    {
        AddPart(new NaturalRightJoinClause(table));
        return this;
    }

    public ISelectBuilderPaginated Offset(int start)
    {
        AddPart(new OffsetClause(start));
        return this;
    }

    public ISelectBuilderOffsetFetch OffsetRows(int start)
    {
        AddPart(new OffsetRowsClause(start));
        return this;
    }

    public ISelectBuilderFrom On(SqlCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    public ISelectBuilderOrderBy OrderBy(params object[] orderByItems)
    {
        OrderByClause orderBy = OrderByClause.Parse(orderByItems);
        // No engine resolves column position 0 (ADR 0007's incomplete construct).
        if (orderBy.HasZeroOrdinal)
        {
            throw new ArgumentException(
                "No engine accepts 0 as an ORDER BY column ordinal; "
                    + "order by a column, an expression, or a positive ordinal instead.");
        }

        AddPart(orderBy);
        return this;
    }

    public ISelectBuilderFrom OuterApply(ISubquery subquery, DerivedTableBase alias)
    {
        AddPart(new OuterApplyClause(subquery, alias));
        return this;
    }

    public ISelectBuilderJoin RightJoin(TableReference table)
    {
        AddPart(new RightJoinClause(table));
        return this;
    }

    // INSERT ... SELECT checks its column list against the select list it receives.
    private protected virtual void OnSelectItems(SqlPart[] selectItems)
    {
    }

    private void AddSelectItems<TClause>(TClause clause)
        where TClause : SqlPart, ISelectItemsClause
    {
        OnSelectItems(clause.SelectItems);
        AddPart(clause);
    }

    public ISelectBuilderSelect Select(params object[] selectItems)
    {
        AddSelectItems(SelectClause.Parse(selectItems));
        return this;
    }

    public ISelectBuilderSelect Select(DistinctKeyword distinct, params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithDistinct.Parse(distinct, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(DistinctOnKeyword distinctOn, params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithDistinct.Parse(distinctOn, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(SqlHints hints, params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithHints.Parse(hints, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithOptions.Parse(hints, distinct, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctOnKeyword distinctOn,
        params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithOptions.Parse(hints, distinctOn, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(TopClause top, params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithTop.Parse(top, selectItems));
        return this;
    }

    public ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        TopClause top,
        params object[] selectItems)
    {
        AddSelectItems(SelectClauseWithDistinctTop.Parse(distinct, top, selectItems));

        return this;
    }

    public ISelectBuilderFrom Using(DbColumn column, params DbColumn[] additionalColumns)
    {
        CollectionGuard.ThrowIfNullElement(
            additionalColumns,
            nameof(additionalColumns),
            "A USING column list must not contain a null column.");

        AddPart(new JoinUsingClause([column, .. additionalColumns], nameof(column)));
        return this;
    }

    public ISelectBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    public ISelectBuilderWithRollup WithRollup()
    {
        AddPart(new WithRollupClause());
        return this;
    }

    // ORDER BY ends a compound (its step offers no set operator), so one after
    // any set operator is the compound's own (#611).
    protected override void AppendNestedParts(
        SqlBuildingBuffer buffer,
        ReadOnlySpan<SqlPart> parts)
    {
        int orderBy = CompoundOrderByIndex(parts);
        if (orderBy < 0)
        {
            buffer.AppendSpaceSeparated(parts);
            return;
        }

        buffer.AppendSpaceSeparated(parts[..orderBy]).AppendSpace().BeginCompoundOrderBy();
        parts[orderBy].Format(buffer);
        buffer.EndCompoundOrderBy();
        if (orderBy + 1 < parts.Length)
        {
            buffer.AppendSpace().AppendSpaceSeparated(parts[(orderBy + 1)..]);
        }
    }

    private static int CompoundOrderByIndex(ReadOnlySpan<SqlPart> parts)
    {
        bool compound = false;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] is UnionOperator or ExceptOperator or IntersectOperator or MinusOperator)
            {
                compound = true;
            }
            else if (compound && parts[i] is OrderByClause)
            {
                return i;
            }
        }

        return -1;
    }

    // The TOP pairings are incomplete constructs on every dialect (ADR 0007); the
    // dialect-scoped checks are ADR 0011's bounded exceptions.
    protected override void Validate(Dbms dbms)
    {
        // MySQL and SQLite reject a bare OFFSET (live-verified). A declaring-interface rule
        // could see it but none exists, and dropping the guard would drop the protection (#569).
        if ((dbms == Dbms.MySql || dbms == Dbms.Sqlite)
            && FindPart<OffsetClause>() is not null
            && FindPart<LimitClause>() is null)
        {
            throw new ArgumentException(
                "MySQL and SQLite accept OFFSET only after LIMIT; "
                    + "add Limit(...) before Offset(...).");
        }

        // MySQL, SQLite and Oracle take a fractional constant sort key as a no-op; PostgreSQL
        // and SQL Server reject it (live-verified), and the analyzer cannot see it (ADR 0011).
        if ((dbms == Dbms.PostgreSql || dbms == Dbms.SqlServer)
            && FindPart<OrderByClause>() is { HasFractionalSortKey: true })
        {
            throw new ArgumentException(
                "PostgreSQL and SQL Server do not accept a non-integer constant as an ORDER BY "
                    + "sort key; order by a column or an expression instead.");
        }

        // PostgreSQL, SQLite and SQL Server read a negative literal as a
        // position and reject it; MySQL and Oracle take it as a constant (#523).
        if ((dbms == Dbms.PostgreSql || dbms == Dbms.Sqlite || dbms == Dbms.SqlServer)
            && FindPart<OrderByClause>() is { HasNegativeOrdinal: true })
        {
            throw new ArgumentException(
                "PostgreSQL, SQLite, and SQL Server do not accept a negative ORDER BY column "
                    + "ordinal; order by a column, an expression, or a positive ordinal instead.");
        }

        if (FindPart<ITopSelectClause>() is null)
        {
            return;
        }

        // TOP is SQL Server's alone, and SQL Server rejects it beside OFFSET/FETCH
        // (TopWithOffsetFetch_IsRejectedByTheEngine), so no target runs the pairing.
        if (FindPart<LimitClause>() is not null
            || FindPart<OffsetClause>() is not null
            || FindPart<OffsetRowsClause>() is not null
            || FindPart<FetchClause>() is not null)
        {
            throw new ArgumentException(
                "TOP cannot be combined with LIMIT, OFFSET, or FETCH; use one or the other.");
        }

        if (!AnyTopWithTies())
        {
            return;
        }

        // No branch takes an ORDER BY of its own, and a compound's sorts the combined
        // result, so a tied branch has no ordering to tie on (#679).
        if (HasSetOperator())
        {
            throw new ArgumentException(
                "TOP ... WITH TIES requires an ORDER BY of its own query block, which a "
                    + "set-operator branch cannot take; put the branch in a derived table.");
        }

        if (FindPart<OrderByClause>() is null)
        {
            throw new ArgumentException("TOP ... WITH TIES requires an ORDER BY clause.");
        }
    }

    // Every block's TOP, not FindPart's first: a compound query's later block
    // carries its own (#569).
    private bool AnyTopWithTies()
    {
        foreach (SqlPart part in PartsSpan)
        {
            if (part is ITopSelectClause { WithTies: true })
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSetOperator()
    {
        foreach (SqlPart part in PartsSpan)
        {
            if (part is UnionOperator or ExceptOperator or IntersectOperator or MinusOperator)
            {
                return true;
            }
        }

        return false;
    }
}
