namespace SqlArtisan.Internal;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);
}
