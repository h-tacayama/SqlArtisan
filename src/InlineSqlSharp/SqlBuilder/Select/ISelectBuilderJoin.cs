namespace InlineSqlSharp;

public interface ISelectBuilderJoin : ISqlBuilder
{
    // Subsequent SQL is the same as the FROM clause.
    ISelectBuilderFrom ON(ICondition condition);
}
