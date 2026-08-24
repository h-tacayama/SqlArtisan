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
        EqualCondition[] assignments = AssignmentResolver.Resolve(
            items, "SET requires at least one assignment.");

        var columns = new SqlExpression[assignments.Length];
        var values = new SqlExpression[assignments.Length];

        for (int i = 0; i < assignments.Length; i++)
        {
            columns[i] = assignments[i].LeftSide;
            values[i] = assignments[i].RightSide;
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
