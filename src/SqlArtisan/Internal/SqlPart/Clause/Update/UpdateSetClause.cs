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
        new(
            AssignmentResolver.Resolve(assignments, "SET requires at least one assignment."),
            state);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // The only position where the alias survives into the SQL, and the
        // shape is not final until .From(t) — so this arm checks at Build().
        AssignmentResolver.ThrowIfDuplicateTarget(_assignments, _state.QualifiesSetTarget);

        buffer.Append($"{Keywords.Set} ");

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
