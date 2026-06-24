namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, string alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, string alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, string alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, string alias);

    ISelectBuilderFrom OuterApply(ISubquery subquery, string alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
