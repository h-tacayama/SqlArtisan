namespace SqlArtisan.Internal;

// The `INSERT (col, ...)` action of a MERGE WHEN NOT MATCHED clause. The VALUES
// list follows as a separate part (reusing InsertValuesClause).
internal sealed class MergeInsertClause(DbColumn[] columns) : SqlPart
{
    private readonly DbColumn[] _columns = columns;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Insert);

        if (_columns.Length > 0)
        {
            buffer.AppendSpace()
                .OpenParenthesis()
                .AppendCsv(_columns)
                .CloseParenthesis();
        }
    }
}
