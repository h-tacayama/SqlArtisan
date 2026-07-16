using SqlArtisan.Internal;

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
    public static ValuesDerivedTable ValuesTable(
        string alias, string[] columnNames, object[][] rows)
    {
        ArgumentException.ThrowIfNullOrEmpty(alias);
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
}
