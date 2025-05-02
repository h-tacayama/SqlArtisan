namespace SqlArtisan;

internal sealed class SqlPartAgent(Action<SqlBuildingBuffer> formatSql) : SqlPart
{
    private readonly Action<SqlBuildingBuffer> _formatSql = formatSql;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _formatSql.Invoke(buffer);
}
