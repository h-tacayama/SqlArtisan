namespace SqlArtisan;

public interface IInsertBuilderInsertInto : ISqlBuilder
{
    IInsertBuilderSet Set(params AbstractEqualityCondition[] assignments);
}
