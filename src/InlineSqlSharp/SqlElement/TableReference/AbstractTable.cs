namespace InlineSqlSharp;

public abstract class AbstractTable : ITableReference
{
    private readonly string _name;
    private readonly string _alias;

    public AbstractTable(string alias)
    {
        _name = GetType().Name;
        _alias = alias;
    }

    public AbstractTable(string tableName, string alias)
    {
        _name = tableName;
        _alias = alias;
    }

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_name)
        .EncloseInDoubleQuotes(_alias);
}
