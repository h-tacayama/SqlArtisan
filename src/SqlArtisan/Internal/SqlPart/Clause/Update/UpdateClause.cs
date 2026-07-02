namespace SqlArtisan.Internal;

internal sealed class UpdateClause(DbTableBase table) : SqlPart
{
    private readonly DbTableBase _table = table;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Update).AppendSpace();
        _table.FormatAsDmlTarget(buffer);
    }
}
