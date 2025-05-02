namespace SqlArtisan;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);
}
