namespace SqlArtisan.Internal;

internal sealed class RegexpOptionsValue(RegexpOptions options) : SqlPart
{
    private readonly RegexpOptions _options = options;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_options.ToSql());
}
