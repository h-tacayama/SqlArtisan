using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// A literal-row source for a MERGE <c>USING</c> —
    /// <c>(VALUES (…),(…)) "alias" (col1, col2)</c>. Each value binds as a
    /// parameter; reference the named columns with <c>.Column(name)</c>.
    /// </summary>
    /// <param name="alias">The source alias.</param>
    /// <param name="columnNames">The source column names, in row-value order.</param>
    /// <param name="rows">The literal rows; each supplies one value per column.</param>
    /// <returns>A <see cref="ValuesDerivedTable"/> usable as a MERGE <c>USING</c> source.</returns>
    /// <remarks>PostgreSQL and SQL Server. Oracle has no <c>VALUES</c> row
    /// constructor in <c>USING</c> — wrap the rows in a subquery source instead.</remarks>
    public static ValuesDerivedTable Values(
        string alias, string[] columnNames, object[][] rows)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        CollectionGuard.ThrowIfEmpty(columnNames, "A VALUES source requires at least one column.");
        CollectionGuard.ThrowIfEmpty(rows, "A VALUES source requires at least one row.");

        foreach (object[] row in rows)
        {
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
    /// <remarks>SQL Server syntax. Other dialects spell this
    /// <see cref="VarSamp(object)"/>.</remarks>
    public static VarFunction Var(object expr) =>
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

    /// <summary>
    /// The <c>VARIANCE(<paramref name="expr"/>)</c> aggregate function.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarianceFunction"/> emitting <c>VARIANCE(expr)</c>.</returns>
    /// <remarks>
    /// MySQL, Oracle, PostgreSQL — but not the same statistic on all three:
    /// MySQL's <c>VARIANCE</c> is the population variance, Oracle's and
    /// PostgreSQL's is the sample variance. For a value that keeps its meaning
    /// across dialects, use <see cref="VarPop(object)"/> or
    /// <see cref="VarSamp(object)"/> instead.
    /// </remarks>
    public static VarianceFunction Variance(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>VARP(<paramref name="expr"/>)</c> aggregate function: the
    /// population variance of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="VarpFunction"/> emitting <c>VARP(expr)</c>.</returns>
    /// <remarks>SQL Server syntax. Other dialects spell this
    /// <see cref="VarPop(object)"/>.</remarks>
    public static VarpFunction Varp(object expr) =>
        new(Resolve(expr));
}
