namespace SqlArtisan.Internal;

/// <summary>
/// The set of join clauses that can follow a table reference: plain joins, lateral joins, and the SQL Server / Oracle <c>APPLY</c> forms.
/// </summary>
public interface IJoinOperator
{
    /// <summary>
    /// Joins a correlated derived table with <c>CROSS APPLY (subquery) alias</c>
    /// (SQL Server / Oracle; PostgreSQL / MySQL spell it
    /// <see cref="CrossJoinLateral(ISubquery, DerivedTableBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    /// <returns>The builder positioned back in the <c>FROM</c> state; the correlation supplies the predicate, so no <c>ON</c> follows.</returns>
    ISelectBuilderFrom CrossApply(ISubquery subquery, DerivedTableBase alias);

    /// <summary>
    /// Appends <c>CROSS JOIN table</c> — the unfiltered Cartesian product, so no <c>ON</c> follows.
    /// </summary>
    /// <param name="table">The table reference to cross-join.</param>
    /// <returns>The builder positioned back in the <c>FROM</c> state; <c>CROSS JOIN</c> takes no <c>ON</c> predicate.</returns>
    ISelectBuilderFrom CrossJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>CROSS JOIN LATERAL (subquery) alias</c>
    /// (MySQL / Oracle 12c+ / PostgreSQL; SQL Server spells it
    /// <see cref="CrossApply(ISubquery, DerivedTableBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    /// <returns>The builder positioned back in the <c>FROM</c> state; the correlation supplies the predicate, so no <c>ON</c> follows.</returns>
    ISelectBuilderFrom CrossJoinLateral(ISubquery subquery, DerivedTableBase alias);

    /// <summary>
    /// Appends <c>FULL JOIN table</c>, keeping unmatched rows from both sides. The join predicate is supplied by the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to full-join.</param>
    /// <returns>The builder positioned to supply the join predicate with <c>On(...)</c>.</returns>
    ISelectBuilderJoin FullJoin(TableReference table);

    /// <summary>
    /// Appends <c>INNER JOIN table</c>, keeping only matched rows. The join predicate is supplied by the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to inner-join.</param>
    /// <returns>The builder positioned to supply the join predicate with <c>On(...)</c>.</returns>
    ISelectBuilderJoin InnerJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>JOIN LATERAL (subquery) alias ON ...</c>,
    /// the join predicate supplied by the following <c>On(...)</c>
    /// (MySQL / Oracle 12c+ / PostgreSQL).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    /// <returns>The builder positioned to supply the join predicate with <c>On(...)</c>.</returns>
    ISelectBuilderJoin JoinLateral(ISubquery subquery, DerivedTableBase alias);

    /// <summary>
    /// Appends <c>LEFT JOIN table</c>, keeping all left-side rows. The join predicate is supplied by the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to left-join.</param>
    /// <returns>The builder positioned to supply the join predicate with <c>On(...)</c>.</returns>
    ISelectBuilderJoin LeftJoin(TableReference table);

    /// <summary>
    /// Joins a correlated derived table with <c>LEFT JOIN LATERAL (subquery) alias ON TRUE</c>
    /// (PostgreSQL / MySQL; the SQL Server / Oracle <c>APPLY</c> form is
    /// <see cref="OuterApply(ISubquery, DerivedTableBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    /// <returns>The builder positioned back in the <c>FROM</c> state; the lateral <c>ON TRUE</c> supplies the predicate, so no <c>ON</c> follows.</returns>
    ISelectBuilderFrom LeftJoinLateral(ISubquery subquery, DerivedTableBase alias);

    /// <summary>
    /// Joins a correlated derived table with <c>OUTER APPLY (subquery) alias</c>
    /// (SQL Server / Oracle; PostgreSQL / MySQL spell it
    /// <see cref="LeftJoinLateral(ISubquery, DerivedTableBase)"/>).
    /// </summary>
    /// <param name="subquery">The derived-table subquery; it may correlate to columns of the preceding tables.</param>
    /// <param name="alias">Names the derived table — a typed <see cref="DerivedTableBase"/> subclass, or an inline <see cref="DerivedTable"/> whose columns you read via <see cref="DerivedTable.Column(string)"/>.</param>
    /// <returns>The builder positioned back in the <c>FROM</c> state; the correlation supplies the predicate, so no <c>ON</c> follows.</returns>
    ISelectBuilderFrom OuterApply(ISubquery subquery, DerivedTableBase alias);

    /// <summary>
    /// Appends <c>RIGHT JOIN table</c>, keeping all right-side rows. The join predicate is supplied by the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to right-join.</param>
    /// <returns>The builder positioned to supply the join predicate with <c>On(...)</c>.</returns>
    ISelectBuilderJoin RightJoin(TableReference table);
}
