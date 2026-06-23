namespace SqlArtisan.Internal;

public interface ISelectBuilderFrom : ISqlBuilder, ISetOperator, IForUpdate, ISubquery, IPagination
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, string alias);

    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, string alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, string alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, string alias);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    ISelectBuilderFrom OuterApply(ISubquery subquery, string alias);

    ISelectBuilderJoin RightJoin(TableReference table);

    ISelectBuilderWhere Where(SqlCondition condition);
}
