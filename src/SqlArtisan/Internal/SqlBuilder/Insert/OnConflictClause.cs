namespace SqlArtisan.Internal;

// PostgreSQL / SQLite UPSERT tail:
//   ON CONFLICT (target...) DO NOTHING
//   ON CONFLICT (target...) DO UPDATE SET col = EXCLUDED.col, ...
// The EXCLUDED vs excluded spelling is resolved in the dialect layer
// (SqlBuildingBuffer.AppendExcludedReference); this clause never branches on
// Dbms. A null _updateColumns means DO NOTHING.
internal sealed class OnConflictClause : SqlPart
{
    private readonly DbColumn[] _conflictTarget;
    private readonly DbColumn[]? _updateColumns;

    internal OnConflictClause(DbColumn[] conflictTarget, DbColumn[]? updateColumns)
    {
        _conflictTarget = conflictTarget;
        _updateColumns = updateColumns;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer
            .Append($"{Keywords.On} {Keywords.Conflict} ")
            .OpenParenthesis()
            .AppendCsv(_conflictTarget)
            .CloseParenthesis()
            .Append($" {Keywords.Do} ");

        if (_updateColumns is null)
        {
            buffer.Append(Keywords.Nothing);
            return;
        }

        buffer.Append($"{Keywords.Update} {Keywords.Set} ");

        for (int i = 0; i < _updateColumns.Length; i++)
        {
            string columnName = _updateColumns[i].Name;

            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer.Append(columnName).Append(" = ").AppendExcludedReference(columnName);
        }
    }
}
