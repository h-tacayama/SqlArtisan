namespace SqlArtisan.Internal;

internal sealed class DeleteClause : SqlPart
{
    private readonly DbTableBase _table;
    private readonly DmlJoinState _state;

    internal DeleteClause(DbTableBase table, DmlJoinState state)
    {
        _table = table;
        _state = state;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // A joined DELETE with a re-listed target leads with the FROM-defined
        // alias alone and introduces the target through the following FROM;
        // every other form keeps the `DELETE FROM target` lead.
        if (_state.TargetRepeatedInFrom)
        {
            buffer.Append($"{Keywords.Delete} ");
            buffer.EncloseInAliasQuotes(_table.CorrelationName);
        }
        else
        {
            buffer.Append($"{Keywords.Delete} {Keywords.From} ");
            _table.FormatAsDmlTarget(buffer);
        }
    }
}
