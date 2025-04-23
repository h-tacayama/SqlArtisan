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

    public new void FormatSql(SqlBuildingBuffer buffer) =>
        base.FormatSql(buffer);

    public SqlStatement Build() => BuildCore();

    public ISelectBuilderFrom CROSS_JOIN(AbstractTableReference table)
    {
        AddElement(new CrossJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom FROM(params AbstractTableReference[] tables)
    {
        AddElement(new FromClause(tables));
        return this;
    }

    public ISelectBuilderJoin FULL_JOIN(AbstractTableReference table)
    {
        AddElement(new FullJoinClause(table));
        return this;
    }

    public ISelectBuilderGroupBy GROUP_BY(params object[] groupByItems)
    {
        AddElement(GroupByClause.Parse(groupByItems));
        return this;
    }

    public ISelectBuilderHaving HAVING(AbstractCondition condition)
    {
        AddElement(new HavingClause(condition));
        return this;
    }

    public ISelectBuilderJoin INNER_JOIN(AbstractTableReference table)
    {
        AddElement(new InnerJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin LEFT_JOIN(AbstractTableReference table)
    {
        AddElement(new LeftJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin RIGHT_JOIN(AbstractTableReference table)
    {
        AddElement(new RightJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom ON(AbstractCondition condition)
    {
        AddElement(new OnClause(condition));
        return this;
    }

    public ISelectBuilderOrderBy ORDER_BY(
        params object[] orderByItems)
    {
        AddElement(OrderByClause.Parse(orderByItems));
        return this;
    }

    public ISelectBuilderSelect SELECT(
        params object[] selectItems)
    {
        AddElement(
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
        AddElement(
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
        AddElement(
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
        AddElement(
            SelectClause.Parse(
                hints,
                distinct,
                selectList));

        return this;
    }

    public ISelectBuildertWhere WHERE(AbstractCondition condition)
    {
        AddElement(new WhereClause(condition));
        return this;
    }
}
