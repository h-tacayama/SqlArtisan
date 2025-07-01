namespace SqlArtisan.Internal;

public interface IDeleteBuilder
{
    IDeleteBuilderDelete DeleteFrom(DbTableBase table);
}
