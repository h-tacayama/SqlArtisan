using System.Diagnostics;

namespace SqlArtisan.Internal;

internal class SelectBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
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
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Except
    {
        get
        {
            AddPart(new ExceptOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator ExceptAll
    {
        get
        {
            AddPart(new ExceptOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Intersect
    {
        get
        {
            AddPart(new IntersectOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator IntersectAll
    {
        get
        {
            AddPart(new IntersectOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Minus
    {
        get
        {
            AddPart(new MinusOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MinusAll
    {
        get
        {
            AddPart(new MinusOperator(true));
            return this;
        }
    }

    protected override string StatementName => Keywords.Select;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator Union
    {
        get
        {
            AddPart(new UnionOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UnionAll
    {
        get
        {
            AddPart(new UnionOperator(true));
            return this;
        }
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

    public ISqlBuilder ForUpdate(LockBehaviorBase? lockBehavior = null)
    {
        AddPart(new ForUpdateClause(lockBehavior));
        return this;
    }

    public ISqlBuilder ForUpdate(OfClause ofClause, LockBehaviorBase? lockBehavior = null)
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
        CollectionGuard.ThrowIfEmpty(tables, nameof(tables), "FROM requires at least one table.");
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
        // No engine resolves column position 0 (ADR 0007's incomplete construct);
        // checked here, not in Parse, because a window's ordering takes the literal.
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

    public ISelectBuilderSelect Select(params object[] selectItems)
    {
        AddPart(SelectClause.Parse(selectItems));
        return this;
    }

    public ISelectBuilderSelect Select(DistinctKeyword distinct, params object[] selectItems)
    {
        AddPart(SelectClauseWithDistinct.Parse(distinct, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(DistinctOnKeyword distinctOn, params object[] selectItems)
    {
        AddPart(SelectClauseWithDistinct.Parse(distinctOn, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(SqlHints hints, params object[] selectItems)
    {
        AddPart(SelectClauseWithHints.Parse(hints, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems)
    {
        AddPart(SelectClauseWithOptions.Parse(hints, distinct, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctOnKeyword distinctOn,
        params object[] selectItems)
    {
        AddPart(SelectClauseWithOptions.Parse(hints, distinctOn, selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(TopClause top, params object[] selectItems)
    {
        AddPart(SelectClauseWithTop.Parse(top, selectItems));
        return this;
    }

    public ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        TopClause top,
        params object[] selectItems)
    {
        AddPart(SelectClauseWithDistinctTop.Parse(distinct, top, selectItems));

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

        if (FindPart<OrderByClause>() is null && AnyTopWithTies())
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
}
