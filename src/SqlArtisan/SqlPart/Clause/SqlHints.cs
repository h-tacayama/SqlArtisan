namespace SqlArtisan;

public sealed class SqlHints : SqlPart
{
    private readonly string _hints;

    internal SqlHints(string hints)
    {
        _hints = hints;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_hints);
}
