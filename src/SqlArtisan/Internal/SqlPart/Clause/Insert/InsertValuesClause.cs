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

    internal void AddRow(object[] values) =>
        _rows.Add(InsertValueResolver.Resolve(values));

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
