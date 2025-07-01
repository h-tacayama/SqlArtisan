namespace SqlArtisan.Internal;

public interface IWithBuilderWith :
    IDeleteBuilder,
    IInsertBuilder,
    ISelectBuilder,
    IUpdateBuilder
{
}
