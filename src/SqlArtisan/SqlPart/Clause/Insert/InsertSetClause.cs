namespace SqlArtisan;

internal sealed class InsertSetClause : SqlPart
{
    private readonly SqlExpression[] _columns;
    private readonly SqlExpression[] _values;

    private InsertSetClause(SqlExpression[] columns, SqlExpression[] values)
    {
        _columns = columns;
        _values = values;
    }

    internal static InsertSetClause Parse(EqualityBasedCondition[] items)
    {
        var columns = new SqlExpression[items.Length];
        var values = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is not EqualityCondition)
            {
                throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {items[i].GetType()}");
            }

            columns[i] = items[i].LeftSide;
            values[i] = items[i].RightSide;
        }

        return new(columns, values);
    }


    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Values)
        .OpenParenthesis()
        .AppendCsv(_values)
        .CloseParenthesis();
}
