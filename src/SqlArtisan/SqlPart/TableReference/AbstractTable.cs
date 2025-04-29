namespace InlineSqlSharp;

public abstract class AbstractTable : AbstractTableReference
{
    private readonly string _tableAlias;

    public AbstractTable(string tableAlias)
    {
        _tableAlias = tableAlias;
    }

    public AbstractTable(string tableName, string tableAlias) : base(tableName)
    {
        _tableAlias = tableAlias;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        base.FormatSql(buffer);
        buffer.AppendSpace();
        buffer.EncloseInDoubleQuotes(_tableAlias);
    }
}
