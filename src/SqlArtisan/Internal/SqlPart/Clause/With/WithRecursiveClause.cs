namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause : SqlPart
{
    // The column list is always emitted and derived eagerly at the
    // WithRecursive(...) call (guards-and-empty-states.md, the WITH RECURSIVE row).
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
