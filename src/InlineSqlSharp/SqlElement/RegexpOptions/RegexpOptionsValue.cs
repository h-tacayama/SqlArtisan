namespace InlineSqlSharp;

internal sealed class RegexpOptionsValue(RegexpOptions options) : ISqlElement
{
    private readonly RegexpOptions _options = options;

    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_options.ToSql());
}
