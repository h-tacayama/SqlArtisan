namespace SqlArtisan;

public abstract class SqlPart
{
    internal abstract void FormatSql(SqlBuildingBuffer buffer);
}
