namespace InlineSqlSharp;

public abstract class AbstractTableReference : AbstractSqlPart
{
    private readonly string _name;

    public AbstractTableReference()
    {
        _name = GetType().Name;
    }

    public AbstractTableReference(string name)
    {
        _name = name;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_name);
}
