namespace SqlArtisan.Internal;

internal sealed class InsertValuesClause : SqlPart
{
    private readonly List<SqlExpression[]> _rows;

    // When set, Format emits only this contiguous slice of rows instead of all
    // of them. BuildBatches uses it to render one chunk per build without
    // copying the row list.
    private (int Offset, int Length)? _window;

    private InsertValuesClause(SqlExpression[] firstRow)
    {
        _rows = [firstRow];
    }

    internal int RowCount => _rows.Count;

    internal int ColumnCount => _rows[0].Length;

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

    internal void SetWindow(int offset, int length) => _window = (offset, length);

    internal void ClearWindow() => _window = null;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        int offset = _window?.Offset ?? 0;
        int length = _window?.Length ?? _rows.Count;

        buffer.Append($"{Keywords.Values} ");

        for (int i = 0; i < length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer
                .OpenParenthesis()
                .AppendCsv(_rows[offset + i])
                .CloseParenthesis();
        }
    }
}
