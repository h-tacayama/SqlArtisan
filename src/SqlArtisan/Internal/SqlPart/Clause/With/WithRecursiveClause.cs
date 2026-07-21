namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause : SqlPart
{
    // The CTE column list (`cte(a, b) AS ...`) is emitted unconditionally,
    // derived from the anchor query block's select items: the engines that
    // accept WITH RECURSIVE (MySQL/PostgreSQL/SQLite) all accept the list, so
    // one uniform shape needs no per-dialect branch. Derivation is eager — the
    // anchor's resolved items are fixed at the WithRecursive(...) call.
    private readonly CommonTableExpressions _ctes;
    private readonly string[][] _columnNames;

    internal WithRecursiveClause(CommonTableExpression[] ctes)
    {
        _ctes = new(ctes);
        _columnNames = DeriveColumnNames(ctes);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _ctes.Format(buffer, $"{Keywords.With} {Keywords.Recursive}", _columnNames);

    private static string[][] DeriveColumnNames(CommonTableExpression[] ctes)
    {
        string[][] columnNames = new string[ctes.Length][];

        for (int i = 0; i < ctes.Length; i++)
        {
            SqlPart[]? selectItems = (ctes[i].Subquery as SelectBuilder)?.FirstSelectItems();
            if (selectItems is null)
            {
                throw NoColumnName();
            }

            string[] names = new string[selectItems.Length];
            for (int j = 0; j < selectItems.Length; j++)
            {
                names[j] = selectItems[j] switch
                {
                    DbColumn column => column.Name,
                    ExpressionAlias alias => alias.Name,
                    _ => throw NoColumnName(),
                };
            }

            columnNames[i] = names;
        }

        return columnNames;
    }

    private static ArgumentException NoColumnName() => new(
        "WITH RECURSIVE requires a name for every column of the CTE's first query block; "
            + "alias the expression with .As(...).");
}
