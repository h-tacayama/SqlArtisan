namespace InlineSqlSharp;

public interface IInsertBuilderInsertInto : ISqlBuilder
{
    IInsertBuilderSet SET(params AbstractEqualityCondition[] assignments);
}
