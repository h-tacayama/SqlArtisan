namespace SqlArtisan.Internal;

public interface IJoinOperator
{
    /// <summary>
    /// Joins a correlated derived table with <c>CROSS APPLY (subquery) alias</c>
    /// (SQL Server / Oracle; PostgreSQL / MySQL spell it
    /// <see cref="CrossJoinLateral(ISubquery, DerivedTableSchemaBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableSchemaBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    ISelectBuilderFrom CrossApply(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderFrom CrossJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>CROSS JOIN LATERAL (subquery) alias</c>
    /// (PostgreSQL / MySQL / Oracle; SQL Server spells it
    /// <see cref="CrossApply(ISubquery, DerivedTableSchemaBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableSchemaBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin FullJoin(TableReference table);

    ISelectBuilderJoin InnerJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>JOIN LATERAL (subquery) alias ON ...</c>,
    /// the join predicate supplied by the following <c>On(...)</c>
    /// (PostgreSQL / MySQL / Oracle).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableSchemaBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    ISelectBuilderJoin JoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin LeftJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>LEFT JOIN LATERAL (subquery) alias ON true</c>
    /// (PostgreSQL / MySQL / Oracle; the SQL Server / Oracle <c>APPLY</c> form is
    /// <see cref="OuterApply(ISubquery, DerivedTableSchemaBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableSchemaBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, DerivedTableSchemaBase alias);

    /// <summary>
    /// Joins a correlated derived table with <c>OUTER APPLY (subquery) alias</c>
    /// (SQL Server / Oracle; PostgreSQL / MySQL spell it
    /// <see cref="LeftJoinLateral(ISubquery, DerivedTableSchemaBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableSchemaBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    ISelectBuilderFrom OuterApply(ISubquery subquery, DerivedTableSchemaBase alias);

    ISelectBuilderJoin RightJoin(TableReference table);
}
