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

    internal static UpdateSetClause Parse(EqualityCondition[] assignments, DmlJoinState state)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(assignments, Keywords.Set);

        // A pair sharing a correlation name, or both unaliased, renders one token either
        // way, so it is rejected here; the rest wait for Build(), once .From(t) is known.
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: true, Keywords.Set);
        return new(resolved, state);
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        AssignmentResolver.ThrowIfDuplicateTarget(
            _assignments, _state.QualifiesSetTarget, Keywords.Set);

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
