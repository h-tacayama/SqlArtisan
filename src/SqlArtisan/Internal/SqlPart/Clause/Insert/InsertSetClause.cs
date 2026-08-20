namespace SqlArtisan.Internal;

internal sealed class InsertSetClause : SqlPart
{
    private readonly SqlExpression[] _columns;
    private readonly SqlExpression[] _values;

    private InsertSetClause(SqlExpression[] columns, SqlExpression[] values)
    {
        _columns = columns;
        _values = values;
    }

    internal static InsertSetClause Parse(EqualityCondition[] items)
    {
        CollectionGuard.ThrowIfEmpty(items, "SET requires at least one assignment.");

        var columns = new SqlExpression[items.Length];
        var values = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is null)
            {
                throw new ArgumentNullException(
                    nameof(items), ExpressionResolver.NullValueMessage);
            }
            else if (items[i] is not EqualCondition)
            {
                throw ExpressionResolver.UnresolvableValue("Assignment", items[i]);
            }

            columns[i] = items[i].LeftSide;
            values[i] = items[i].RightSide;
        }

        return new(columns, values);
    }


    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis()
        .AppendUnqualifiedColumnsCsv(_columns)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Values)
        .OpenParenthesis()
        .AppendCsv(_values)
        .CloseParenthesis();
}
