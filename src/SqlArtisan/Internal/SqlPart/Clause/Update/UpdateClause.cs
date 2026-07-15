namespace SqlArtisan.Internal;

internal sealed class UpdateClause(DbTableBase table, DmlJoinState state) : SqlPart
{
    private readonly DbTableBase _table = table;
    private readonly DmlJoinState _state = state;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Update} ");

        // SQL Server's joined form leads with the FROM-defined alias alone
        // (`UPDATE "t"`); the target itself is re-listed in the FROM clause.
        if (_state.TargetRepeatedInFrom)
        {
            buffer.EncloseInAliasQuotes(_table.CorrelationName);
        }
        else
        {
            _table.FormatAsDmlTarget(buffer);
        }
    }
}
