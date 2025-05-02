namespace SqlArtisan;

public interface ISelectBuilderFrom : ISqlBuilder, ISetOperator, ISubquery
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    ISelectBuilderJoin RightJoin(TableReference table);

    ISelectBuildertWhere Where(SqlCondition condition);
}
