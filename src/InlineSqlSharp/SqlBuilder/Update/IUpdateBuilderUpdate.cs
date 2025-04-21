namespace InlineSqlSharp;

public interface IUpdateBuilderUpdate : ISqlBuilder
{
    IUpdateBuilderSet SET(params AbstractEqualityCondition[] assignments);
}
