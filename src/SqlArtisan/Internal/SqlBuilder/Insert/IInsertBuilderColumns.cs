namespace SqlArtisan.Internal;

public interface IInsertBuilderColumns : ISqlBuilder, ISelectBuilder, IWithBuilder
{
    IInsertBuilderValues Values(params object[] values);
}
