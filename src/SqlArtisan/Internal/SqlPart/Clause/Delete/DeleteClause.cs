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
        // A re-listed target leads with the FROM-defined alias alone (see
        // DmlJoinState.TargetRepeatedInFrom); every other form keeps `DELETE FROM target`.
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
