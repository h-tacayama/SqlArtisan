namespace InlineSqlSharp;

public sealed class InsertSetClause : ISqlElement
{
    private readonly EqualityCondition[] _assignments;

    internal InsertSetClause(params IAssignment[] assignments)
    {
        _assignments = ArgumentValidator.ThrowIfNotEqualityCondition(assignments);
    }

    public void FormatSql(SqlBuildingBuffer buffer)
    {
        IExpr[] leftSides = new IExpr[_assignments.Length];
        IExpr[] rightSides = new IExpr[_assignments.Length];

        for (int i = 0; i < _assignments.Length; i++)
        {
            leftSides[i] = _assignments[i].LeftSide;
            rightSides[i] = _assignments[i].RightSide;
        }

        buffer.OpenParenthesis()
            .AppendCsv(leftSides)
            .CloseParenthesis()
            .EncloseInSpaces(Keywords.VALUES)
            .OpenParenthesis()
            .AppendCsv(rightSides)
            .CloseParenthesis();
    }
}
