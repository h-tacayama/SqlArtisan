namespace SqlArtisan.Internal;

internal sealed class UpdateSetClause : SqlPart
{
    private readonly EqualCondition[] _assignments;
    private readonly DmlJoinState _state;

    private UpdateSetClause(EqualCondition[] assignments, DmlJoinState state)
    {
        _assignments = assignments;
        _state = state;
    }

    internal static UpdateSetClause Parse(EqualityCondition[] assignments, DmlJoinState state) =>
        new(AssignmentResolver.Resolve(assignments, "SET requires at least one assignment."), state);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Set} ");

        // A joined UPDATE on SQL Server / MySQL qualifies the SET target
        // (`SET t.col = ...`); PostgreSQL's UPDATE ... FROM keeps it unqualified.
        if (_state.QualifiesSetTarget)
        {
            buffer.AppendCsv(_assignments);
        }
        else
        {
            buffer.AppendAssignmentsCsv(_assignments);
        }
    }
}
