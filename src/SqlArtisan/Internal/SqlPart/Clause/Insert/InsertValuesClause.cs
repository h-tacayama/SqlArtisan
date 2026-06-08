namespace SqlArtisan.Internal;

internal sealed class InsertValuesClause : SqlPart
{
    private readonly List<SqlExpression[]> _rows;

    private InsertValuesClause(SqlExpression[] firstRow)
    {
        _rows = [firstRow];
    }

    internal static InsertValuesClause Parse(object[] values) =>
        new(InsertValueResolver.Resolve(values));

    internal void AddRow(object[] values)
    {
        SqlExpression[] row = InsertValueResolver.Resolve(values);

        if (row.Length != _rows[0].Length)
        {
            throw new ArgumentException(
                "All rows in a multi-row INSERT must have the same number of values. " +
                $"The first row has {_rows[0].Length}, but this row has {row.Length}.");
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
