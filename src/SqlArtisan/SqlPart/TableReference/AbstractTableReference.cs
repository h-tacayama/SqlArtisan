namespace InlineSqlSharp;

public abstract class AbstractTableReference : AbstractSqlPart
{
    private readonly string _tableName;

    public AbstractTableReference()
    {
        _tableName = GetType().Name;
    }

    public AbstractTableReference(string tableName)
    {
        _tableName = tableName;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_tableName);
}
