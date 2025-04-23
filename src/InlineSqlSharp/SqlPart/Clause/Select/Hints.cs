namespace InlineSqlSharp;

public sealed class Hints : AbstractSqlPart
{
    private readonly string _hints;

    internal Hints(string hints)
    {
        _hints = hints;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_hints);
}
