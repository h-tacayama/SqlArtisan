using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// A literal-row source for <c>FROM</c>, a join, or a MERGE <c>USING</c> —
    /// <c>(VALUES (…),(…)) "alias" (col1, col2)</c>. Literal values bind as
    /// parameters; reference the named columns with <c>.Column(name)</c>.
    /// </summary>
    /// <param name="alias">The source alias.</param>
    /// <param name="columnNames">The source column names, in row-value order.</param>
    /// <param name="rows">The literal rows; each supplies one value per column.</param>
    /// <returns>A <see cref="ValuesDerivedTable"/> usable wherever a derived table is.</returns>
    /// <remarks>PostgreSQL (15+) and SQL Server. Oracle has no <c>VALUES</c> row
    /// constructor in <c>USING</c> — wrap the rows in a subquery source instead.</remarks>
    public static ValuesDerivedTable Values(
        string alias, string[] columnNames, object[][] rows)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        CollectionGuard.ThrowIfEmpty(
            columnNames, nameof(columnNames), "A VALUES source requires at least one column.");
        CollectionGuard.ThrowIfEmpty(
            rows, nameof(rows), "A VALUES source requires at least one row.");

        foreach (string columnName in columnNames)
        {
            StringGuard.ThrowIfNullOrWhiteSpace(
                columnName, "A VALUES source requires a name for every column.");
        }

        if (CommonTableExpression.HasDuplicateName(columnNames))
        {
            throw new ArgumentException(
                "A VALUES source requires a distinct name for every column.");
        }

        foreach (object[] row in rows)
        {
            if (row is null)
            {
                throw new ArgumentNullException(
                    nameof(rows), "A VALUES source must not contain a null row.");
            }

            if (row.Length != columnNames.Length)
            {
                throw new ArgumentException(
                    "Every row of a VALUES source must supply one value per column; "
                        + $"the column list has {columnNames.Length}, but a row has {row.Length}.");
            }
        }

        InsertValuesClause body = InsertValuesClause.Parse(rows[0]);
        for (int i = 1; i < rows.Length; i++)
        {
            body.AddRow(rows[i]);
        }

        return new ValuesDerivedTable(alias, columnNames, body);
    }

    /// <summary>
    /// The <c>VAR(<paramref name="expr"/>)</c> aggregate function: the sample
    /// variance of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarFunction"/> emitting <c>VAR(expr)</c>.</returns>
    /// <remarks>SQL Server syntax. MySQL, Oracle, and PostgreSQL spell this
    /// <see cref="VarSamp(object)"/>.</remarks>
    public static VarFunction Var(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>VARIANCE(<paramref name="expr"/>)</c> aggregate function.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarianceFunction"/> emitting <c>VARIANCE(expr)</c>.</returns>
    /// <remarks>
    /// MySQL, Oracle, and PostgreSQL syntax; MySQL computes the population statistic,
    /// the other two the sample. <see cref="VarPop(object)"/> and
    /// <see cref="VarSamp(object)"/> name the statistic on every dialect.
    /// </remarks>
    public static VarianceFunction Variance(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>VARP(<paramref name="expr"/>)</c> aggregate function: the
    /// population variance of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarpFunction"/> emitting <c>VARP(expr)</c>.</returns>
    /// <remarks>SQL Server syntax. MySQL, Oracle, and PostgreSQL spell this
    /// <see cref="VarPop(object)"/>.</remarks>
    public static VarpFunction Varp(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>VAR_POP(<paramref name="expr"/>)</c> aggregate function: the
    /// population variance of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarPopFunction"/> emitting <c>VAR_POP(expr)</c>.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL. SQL Server spells this
    /// <see cref="Varp(object)"/>.</remarks>
    public static VarPopFunction VarPop(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>VAR_SAMP(<paramref name="expr"/>)</c> aggregate function: the
    /// sample variance of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarSampFunction"/> emitting <c>VAR_SAMP(expr)</c>.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL. SQL Server spells this
    /// <see cref="Var(object)"/>.</remarks>
    public static VarSampFunction VarSamp(object expr) =>
        new(Resolve(expr));
}
