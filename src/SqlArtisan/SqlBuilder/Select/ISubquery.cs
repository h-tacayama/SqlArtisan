namespace SqlArtisan;

public interface ISubquery
{
    internal void FormatSql(SqlBuildingBuffer buffer);
}
