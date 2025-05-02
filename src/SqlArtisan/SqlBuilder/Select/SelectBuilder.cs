using System.Diagnostics;

namespace SqlArtisan;

internal class SelectBuilder(SqlPart part) :
    SqlBuilderBase(part),
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

    public new void Format(SqlBuildingBuffer buffer) =>
        base.Format(buffer);

    public SqlStatement Build() => BuildCore();

    public ISelectBuilderFrom CrossJoin(TableReference table)
    {
        AddPart(new CrossJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom From(params TableReference[] tables)
    {
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

    public ISelectBuilderJoin LeftJoin(TableReference table)
    {
        AddPart(new LeftJoinClause(table));
        return this;
    }

    public ISelectBuilderJoin RightJoin(TableReference table)
    {
        AddPart(new RightJoinClause(table));
        return this;
    }

    public ISelectBuilderFrom On(SqlCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    public ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems)
    {
        AddPart(OrderByClause.Parse(orderByItems));
        return this;
    }

    public ISelectBuilderSelect Select(
        params object[] selectItems)
    {
        AddPart(SelectClause.Parse(selectItems));
        return this;
    }

    public ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems)
    {
        AddPart(
            SelectClauseWithDistinct.Parse(
                distinct,
                selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems)
    {
        AddPart(
            SelectClauseWithHints.Parse(
                hints,
                selectItems));

        return this;
    }

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectList)
    {
        AddPart(
            SelectClauseWithOptions.Parse(
                hints,
                distinct,
                selectList));

        return this;
    }

    public ISelectBuildertWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
