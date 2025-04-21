namespace InlineSqlSharp;

internal sealed class RegexpOptionsValue(RegexpOptions options) : AbstractSqlPart
{
    private readonly RegexpOptions _options = options;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_options.ToSql());
}
