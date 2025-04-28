namespace InlineSqlSharp;

internal sealed class InsertSetClause : AbstractSqlPart
{
    private readonly AbstractExpr[] _columns;
    private readonly AbstractExpr[] _values;

    private InsertSetClause(AbstractExpr[] columns, AbstractExpr[] values)
    {
        _columns = columns;
        _values = values;
    }

    internal static InsertSetClause Parse(AbstractEqualityCondition[] items)
    {
        var columns = new AbstractExpr[items.Length];
        var values = new AbstractExpr[items.Length];

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
