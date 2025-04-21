namespace InlineSqlSharp;

public interface ISelectBuilderFrom : ISqlBuilder, ISetOperator, ISubquery
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom CROSS_JOIN(AbstractTableReference table);

    ISelectBuilderJoin FULL_JOIN(AbstractTableReference table);

    ISelectBuilderGroupBy GROUP_BY(params object[] groupByItems);

    ISelectBuilderJoin INNER_JOIN(AbstractTableReference table);

    ISelectBuilderJoin LEFT_JOIN(AbstractTableReference table);

    ISelectBuilderOrderBy ORDER_BY(
        params object[] orderByItems);

    ISelectBuilderJoin RIGHT_JOIN(AbstractTableReference table);

    ISelectBuildertWhere WHERE(AbstractCondition condition);
}
