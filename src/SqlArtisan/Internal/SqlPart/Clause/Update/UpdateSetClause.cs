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

    internal static UpdateSetClause Parse(EqualityCondition[] items, DmlJoinState state)
    {
        CollectionGuard.ThrowIfEmpty(items, "SET requires at least one assignment.");

        var assignments = new EqualCondition[items.Length];

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

            assignments[i] = (EqualCondition)items[i];
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
