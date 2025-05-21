namespace SqlArtisan.Internal;

public interface IInsertBuilderColumns : ISqlBuilder, ISelectBuilder
{
    IInsertBuilderValues Values(params object[] values);
}
