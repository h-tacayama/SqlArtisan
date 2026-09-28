namespace SqlArtisan.Internal;

internal sealed class InsertValuesClause : SqlPart
{
    private readonly List<SqlExpression[]> _rows;

    private InsertValuesClause(SqlExpression[] firstRow)
    {
        _rows = [firstRow];
    }

    // A VALUES derived table borrows this clause but names its own position (#569).
    internal static InsertValuesClause Parse(object[] values, string position = "InsertValue") =>
        new(InsertValueResolver.Resolve(values, position));

    internal static InsertValuesClause FromResolved(SqlExpression[] firstRow) => new(firstRow);

    internal int RowWidth => _rows[0].Length;

    internal void AddResolvedRow(SqlExpression[] row) => _rows.Add(row);

    internal void AddRow(object[] values, string position = "InsertValue")
    {
        SqlExpression[] row = InsertValueResolver.Resolve(values, position);

        if (row.Length != _rows[0].Length)
        {
            throw new ArgumentException(
                "All rows in a multi-row INSERT must have the same number of values; " +
                $"the first row has {_rows[0].Length}, but this row has {row.Length}.");
        }

        _rows.Add(row);
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Values} ");

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer
                .OpenParenthesis()
                .AppendCsv(_rows[i])
                .CloseParenthesis();
        }
    }
}
