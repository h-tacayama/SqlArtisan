using System.Diagnostics;

namespace InlineSqlSharp;

internal class SelectBuilder :
    AbstractSqlBuilder,
    ISelectBuilderFrom,
    ISelectBuilderGroupBy,
    ISelectBuilderHaving,
    ISelectBuilderSetOperator,
    ISelectBuilderJoin,
    ISelectBuilderOrderBy,
    ISelectBuilderSelect,
    ISelectBuildertWhere
{
    internal SelectBuilder(ISqlElement sqlElement) : base(sqlElement)
    {
    }

    internal SelectBuilder(SelectClause selectClause) : base(selectClause)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator EXCEPT
    {
        get
        {
            AddElement(new ExceptOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator EXCEPT_ALL
    {
        get
        {
            AddElement(new ExceptOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator INTERSECT
    {
        get
        {
            AddElement(new IntersectOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator INTERSECT_ALL
    {
        get
        {
            AddElement(new IntersectOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MINUS
    {
        get
        {
            AddElement(new MinusOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MINUS_ALL
    {
        get
        {
            AddElement(new MinusOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UNION
    {
        get
        {
            AddElement(new UnionOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UNION_ALL
    {
        get
        {
            AddElement(new UnionOperator(true));
            return this;
        }
    }

    public void FormatSql(SqlBuildingBuffer buffer) =>
        FormatAsSubquery(ref buffer);

    public SqlStatement Build() => BuildCore();

    public ISelectBuilderFrom CROSS_JOIN(ITableReference table)
    {
        AddElement(new CrossJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom FROM(params ITableReference[] tables)
    {
        AddElement(new FromClause(tables));
        return this;
    }

    public ISelectBuilderJoin FULL_JOIN(ITableReference table)
    {
        AddElement(new FullJoinClause(table));
        return this;
    }

    public ISelectBuilderGroupBy GROUP_BY(params IExpr[] groupingExpressions)
    {
        AddElement(new GroupByClause(groupingExpressions));
        return this;
    }

    public ISelectBuilderHaving HAVING(ICondition condition)
    {
        AddElement(new HavingClause(condition));
        return this;
    }

    public ISelectBuilderJoin INNER_JOIN(ITableReference table)
    {
        AddElement(new InnerJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin LEFT_JOIN(ITableReference table)
    {
        AddElement(new LeftJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin RIGHT_JOIN(ITableReference table)
    {
        AddElement(new RightJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom ON(ICondition condition)
    {
        AddElement(new OnClause(condition));
        return this;
    }

    public ISelectBuilderOrderBy ORDER_BY(
        params IExprOrAliasOrSortOrder[] sortExpressions)
    {
        AddElement(new OrderByClause(sortExpressions));
        return this;
    }

    public ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList)
    {
        AddElement(new SelectClause(Hints.None, AllOrDistinct.All, selectList));
        return this;
    }

    public ISelectBuilderSelect SELECT(
        AllOrDistinct allOrDistinct,
        params IExprOrAlias[] selectList)
    {
        AddElement(new SelectClause(Hints.None, allOrDistinct, selectList));
        return this;
    }

    public ISelectBuilderSelect SELECT(
        Hints hints,
        params IExprOrAlias[] selectList)
    {
        AddElement(new SelectClause(hints, AllOrDistinct.All, selectList));
        return this;
    }

    public ISelectBuilderSelect SELECT(
        Hints hints,
        AllOrDistinct allOrDistinct,
        params IExprOrAlias[] selectList)
    {
        AddElement(new SelectClause(hints, allOrDistinct, selectList));
        return this;
    }

    public ISelectBuildertWhere WHERE(ICondition condition)
    {
        AddElement(new WhereClause(condition));
        return this;
    }
}
