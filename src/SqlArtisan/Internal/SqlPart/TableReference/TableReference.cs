namespace SqlArtisan.Internal;

public abstract class TableReference : SqlPart
{
    private readonly string _tableName;

    public TableReference()
    {
        _tableName = GetType().Name;
    }

    public TableReference(string tableName)
    {
        _tableName = tableName;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_tableName);
}
