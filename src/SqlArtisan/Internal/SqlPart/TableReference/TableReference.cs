namespace SqlArtisan.Internal;

public abstract class TableReference : SqlPart
{
    private protected readonly string _name;

    public TableReference()
    {
        _name = GetType().Name;
    }

    public TableReference(string name)
    {
        _name = name;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_name);
}
