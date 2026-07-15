namespace SqlArtisan.Internal;

internal sealed class UpdateSetClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;
    private readonly DmlJoinState _state;

    private UpdateSetClause(EqualityCondition[] assignments, DmlJoinState state)
    {
        _assignments = assignments;
        _state = state;
    }

    internal static UpdateSetClause Parse(EqualityBasedCondition[] items, DmlJoinState state)
    {
        var assignments = new EqualityCondition[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] is not EqualityCondition)
            {
                throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {items[i].GetType()}");
            }

            assignments[i] = (EqualityCondition)items[i];
        }

        return new(assignments, state);
    }

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
