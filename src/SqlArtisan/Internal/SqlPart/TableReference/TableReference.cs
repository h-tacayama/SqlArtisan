namespace SqlArtisan.Internal;

public abstract class TableReference : SqlPart
{
    public TableReference()
    {
        Name = GetType().Name;
    }

    public TableReference(string tableName)
    {
        Name = tableName;
    }

    private protected string Name { get; }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Name);
}
