namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, AdHocDerivedTable alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, AdHocDerivedTable alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, AdHocDerivedTable alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, AdHocDerivedTable alias);

    ISelectBuilderFrom OuterApply(ISubquery subquery, AdHocDerivedTable alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
