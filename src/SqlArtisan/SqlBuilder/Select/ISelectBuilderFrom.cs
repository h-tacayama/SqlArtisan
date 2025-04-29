namespace InlineSqlSharp;

public interface ISelectBuilderFrom : ISqlBuilder, ISetOperator, ISubquery
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom CrossJoin(AbstractTableReference table);

    ISelectBuilderJoin FullJoin(AbstractTableReference table);

    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderJoin InnerJoin(AbstractTableReference table);

    ISelectBuilderJoin LeftJoin(AbstractTableReference table);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    ISelectBuilderJoin RightJoin(AbstractTableReference table);

    ISelectBuildertWhere Where(AbstractCondition condition);
}
