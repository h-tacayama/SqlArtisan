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

    internal static InsertSetClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(
            assignments, "SET requires at least one assignment.");

        var columns = new SqlExpression[resolved.Length];
        var values = new SqlExpression[resolved.Length];

        for (int i = 0; i < resolved.Length; i++)
        {
            columns[i] = resolved[i].LeftSide;
            values[i] = resolved[i].RightSide;
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
