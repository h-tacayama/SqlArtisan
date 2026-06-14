namespace SqlArtisan.Internal;

// MySQL UPSERT tail, emitted right after VALUES (...):
//   AS new ON DUPLICATE KEY UPDATE col = new.col, ...
// Uses the 8.0.19+ row-alias form (AS new ... new.col) to avoid the deprecated
// VALUES(col) reference. The "AS new" sits between VALUES and ON DUPLICATE, so
// this clause carries it rather than mutating the VALUES clause — a small but
// telling structural divergence from ON CONFLICT.
internal sealed class OnDuplicateKeyUpdateClause : SqlPart
{
    private const string RowAlias = "new";

    private readonly DbColumn[] _updateColumns;

    internal OnDuplicateKeyUpdateClause(DbColumn[] updateColumns)
    {
        _updateColumns = updateColumns;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(
            $"{Keywords.As} {RowAlias} {Keywords.On} {Keywords.Duplicate} " +
            $"{Keywords.Key} {Keywords.Update} ");

        for (int i = 0; i < _updateColumns.Length; i++)
        {
            string columnName = _updateColumns[i].Name;

            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer.Append(columnName).Append($" = {RowAlias}.").Append(columnName);
        }
    }
}
