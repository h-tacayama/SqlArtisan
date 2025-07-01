namespace SqlArtisan.Internal;

public interface IInsertBuilder
{
    IInsertBuilderTable InsertInto(DbTableBase table);

    IInsertBuilderColumns InsertInto(DbTableBase table, params DbColumn[] columns);
}
