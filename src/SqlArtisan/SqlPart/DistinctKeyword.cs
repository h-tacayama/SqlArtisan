namespace SqlArtisan;

public sealed class DistinctKeyword : SqlPart
{
    internal DistinctKeyword() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Distinct);
}
