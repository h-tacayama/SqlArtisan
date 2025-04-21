namespace InlineSqlSharp;

public abstract class AbstractTable : AbstractTableReference
{
    private readonly string _alias;

    public AbstractTable(string alias)
    {
        _alias = alias;
    }

    public AbstractTable(string name, string alias) : base(name)
    {
        _alias = alias;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        base.FormatSql(buffer);
        buffer.AppendSpace();
        buffer.EncloseInDoubleQuotes(_alias);
    }
}
