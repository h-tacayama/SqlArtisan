namespace SqlArtisan;

public interface IInsertBuilderInsertInto : ISqlBuilder, ISelectBuilder
{
    IInsertBuilderValues Values(params object[] values);
}
