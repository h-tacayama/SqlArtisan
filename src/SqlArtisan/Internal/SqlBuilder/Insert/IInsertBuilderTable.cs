namespace SqlArtisan.Internal;

public interface IInsertBuilderTable : ISqlBuilder
{
    IInsertBuilderSet Set(params EqualityBasedCondition[] assignments);

    IInsertBuilderValues Values(params object[] values);
}
