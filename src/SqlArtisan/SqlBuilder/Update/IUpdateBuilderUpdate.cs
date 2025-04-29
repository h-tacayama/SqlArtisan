namespace SqlArtisan;

public interface IUpdateBuilderUpdate : ISqlBuilder
{
    IUpdateBuilderSet Set(params AbstractEqualityCondition[] assignments);
}
