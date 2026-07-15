namespace SqlArtisan.Internal;

public sealed class DeleteClause : SqlPart
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
        // SQL Server / MySQL joined DELETE leads with the FROM-defined alias
        // (`DELETE "t"`) and introduces the target through the following FROM;
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
