namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, DerivedTable alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, DerivedTable alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, DerivedTable alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, DerivedTable alias);

    ISelectBuilderFrom OuterApply(ISubquery subquery, DerivedTable alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
