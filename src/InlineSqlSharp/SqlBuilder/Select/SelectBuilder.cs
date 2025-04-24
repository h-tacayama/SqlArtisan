using System.Diagnostics;

namespace InlineSqlSharp;

internal class SelectBuilder(AbstractSqlPart part) :
    AbstractSqlBuilder(part),
    ISelectBuilderFrom,
    ISelectBuilderGroupBy,
    ISelectBuilderHaving,
    ISelectBuilderSetOperator,
    ISelectBuilderJoin,
    ISelectBuilderOrderBy,
    ISelectBuilderSelect,
    ISelectBuildertWhere
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator EXCEPT
    {
        get
        {
            AddPart(new ExceptOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator EXCEPT_ALL
    {
        get
        {
            AddPart(new ExceptOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator INTERSECT
    {
        get
        {
            AddPart(new IntersectOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator INTERSECT_ALL
    {
        get
        {
            AddPart(new IntersectOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MINUS
    {
        get
        {
            AddPart(new MinusOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator MINUS_ALL
    {
        get
        {
            AddPart(new MinusOperator(true));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UNION
    {
        get
        {
            AddPart(new UnionOperator(false));
            return this;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ISelectBuilderSetOperator UNION_ALL
    {
        get
        {
            AddPart(new UnionOperator(true));
            return this;
        }
    }

    public new void FormatSql(SqlBuildingBuffer buffer) =>
        base.FormatSql(buffer);

    public SqlStatement Build() => BuildCore();

    public ISelectBuilderFrom CROSS_JOIN(AbstractTableReference table)
    {
        AddPart(new CrossJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom FROM(params AbstractTableReference[] tables)
    {
        AddPart(new FromClause(tables));
        return this;
    }

    public ISelectBuilderJoin FULL_JOIN(AbstractTableReference table)
    {
        AddPart(new FullJoinClause(table));
        return this;
    }

    public ISelectBuilderGroupBy GROUP_BY(params object[] groupByItems)
    {
        AddPart(GroupByClause.Parse(groupByItems));
        return this;
    }

    public ISelectBuilderHaving HAVING(AbstractCondition condition)
    {
        AddPart(new HavingClause(condition));
        return this;
    }

    public ISelectBuilderJoin INNER_JOIN(AbstractTableReference table)
    {
        AddPart(new InnerJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin LEFT_JOIN(AbstractTableReference table)
    {
        AddPart(new LeftJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin RIGHT_JOIN(AbstractTableReference table)
    {
        AddPart(new RightJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom ON(AbstractCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    public ISelectBuilderOrderBy ORDER_BY(
        params object[] orderByItems)
    {
        AddPart(OrderByClause.Parse(orderByItems));
        return this;
    }

    public ISelectBuilderSelect SELECT(
        params object[] selectItems)
    {
        AddPart(
            SelectClause.Parse(
                null,
                null,
                selectItems));

        return this;
    }

    public ISelectBuilderSelect SELECT(
        Distinct distinct,
        params object[] selectItems)
    {
        AddPart(
            SelectClause.Parse(
                null,
                distinct,
                selectItems));

        return this;
    }

    public ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems)
    {
        AddPart(
            SelectClause.Parse(
                hints,
                null,
                selectItems));

        return this;
    }

    public ISelectBuilderSelect SELECT(
        Hints hints,
        Distinct distinct,
        params object[] selectList)
    {
        AddPart(
            SelectClause.Parse(
                hints,
                distinct,
                selectList));

        return this;
    }

    public ISelectBuildertWhere WHERE(AbstractCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
