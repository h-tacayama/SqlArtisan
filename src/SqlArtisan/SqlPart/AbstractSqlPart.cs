namespace SqlArtisan;

public abstract class AbstractSqlPart
{
    internal abstract void FormatSql(SqlBuildingBuffer buffer);
}
