namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause : SqlPart
{
    // The CTE column list (`cte(a, b) AS ...`) is emitted unconditionally: every
    // engine that accepts WITH RECURSIVE accepts the list, so one uniform shape
    // needs no per-dialect branch. Deriving it is eager — the anchor's resolved
    // select items are fixed at the WithRecursive(...) call.
    private readonly CommonTableExpressions _ctes;
    private readonly CteColumnName[][] _columnNames;

    internal WithRecursiveClause(CommonTableExpression[] ctes)
    {
        _ctes = new(ctes);
        _columnNames = DeriveColumnNames(ctes);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _ctes.Format(buffer, $"{Keywords.With} {Keywords.Recursive}", _columnNames);

    private static CteColumnName[][] DeriveColumnNames(CommonTableExpression[] ctes)
    {
        CteColumnName[][] columnNames = new CteColumnName[ctes.Length][];

        for (int i = 0; i < ctes.Length; i++)
        {
            columnNames[i] = ctes[i].TryDeriveColumnNames() ?? throw NoColumnName();
            if (CommonTableExpression.HasDuplicateName(columnNames[i]))
            {
                throw new ArgumentException(
                    "WITH RECURSIVE requires a distinct name for every column of the CTE's "
                        + "first query block; alias the duplicate with .As(...).");
            }
        }

        return columnNames;
    }

    private static ArgumentException NoColumnName() => new(
        "WITH RECURSIVE requires a name for every column of the CTE's first query block; "
            + "alias the expression with .As(...).");
}
