namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    ISelectBuilderFrom CrossApply(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    ISelectBuilderJoin JoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderFrom OuterApply(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
