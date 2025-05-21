namespace SqlArtisan.Internal;

public interface IUpdateBuilderUpdate : ISqlBuilder
{
    IUpdateBuilderSet Set(params EqualityBasedCondition[] assignments);
}
