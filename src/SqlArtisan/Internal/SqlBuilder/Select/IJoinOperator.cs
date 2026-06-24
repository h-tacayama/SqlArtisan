namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, UntypedDerivedTable alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, UntypedDerivedTable alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, UntypedDerivedTable alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, UntypedDerivedTable alias);

    ISelectBuilderFrom OuterApply(ISubquery subquery, UntypedDerivedTable alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
