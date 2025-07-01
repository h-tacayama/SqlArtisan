namespace SqlArtisan.Internal;

public interface IUpdateBuilder
{
    IUpdateBuilderUpdate Update(DbTableBase table);
}
