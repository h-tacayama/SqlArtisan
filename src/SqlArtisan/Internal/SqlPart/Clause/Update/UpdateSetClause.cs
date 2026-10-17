namespace SqlArtisan.Internal;

internal sealed class UpdateSetClause : SqlPart
{
    private readonly EqualCondition[] _assignments;
    private readonly DbTableBase _target;
    private readonly DmlJoinState _state;

    private UpdateSetClause(EqualCondition[] assignments, DbTableBase target, DmlJoinState state)
    {
        _assignments = assignments;
        _target = target;
        _state = state;
    }

    internal static UpdateSetClause Parse(
        EqualityCondition[] assignments,
        DbTableBase target,
        DmlJoinState state)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(assignments, Keywords.Set);

        // A pair sharing a correlation name, or both unaliased, renders one token either
        // way, so it is rejected here; the rest wait for Build(), once .From(t) is known.
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: true, Keywords.Set);
        return new(resolved, target, state);
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        AssignmentResolver.ThrowIfDuplicateTarget(
            _assignments, _state.QualifiesSetTarget, Keywords.Set);

        buffer.Append($"{Keywords.Set} ");

        if (_state.QualifiesSetTarget)
        {
            // This arm names each column's owner, and MySQL's joined UPDATE sets another
            // table's column that way; an unaliased owner renders bare, as the other arm does.
            AssignmentResolver.ThrowIfUnaliasedOwner(_assignments);
            buffer.AppendCsv(_assignments);
        }
        else
        {
            AssignmentResolver.ThrowIfNotTargetColumn(_assignments, _target, Keywords.Set);
            buffer.AppendAssignmentsCsv(_assignments);
        }
    }
}
