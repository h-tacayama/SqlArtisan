namespace SqlArtisan.Internal;

// A column list fixed at the call site that names one column twice is a
// call-site defect (#225's class, ADR 0011): engines that take it drop or
// overwrite one entry silently (guards-and-empty-states.md names which).
internal static class ColumnListGuard
{
    internal static void ThrowIfDuplicate(DbColumn[] columns, string message)
    {
        for (int i = 1; i < columns.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (columns[i].Name == columns[j].Name)
                {
                    throw new ArgumentException(message);
                }
            }
        }
    }

    internal static void ThrowIfForeignColumn(DbColumn[] columns, DbTableBase table, string message)
    {
        for (int i = 0; i < columns.Length; i++)
        {
            if (!IsColumnOf(columns[i], table))
            {
                throw new ArgumentException(message);
            }
        }
    }

    // A column list or bare SET target renders a column by its name alone, so another
    // relation's column would name the table's same-named one (#674). Keyed on the
    // table name, so another handle of the same table stays legal.
    internal static bool IsColumnOf(DbColumn column, DbTableBase table) =>
        ReferenceEquals(column.Owner, table)
        || (column.Owner is DbTableBase && TableReference.NamesSameTable(column.Owner, table));
}
