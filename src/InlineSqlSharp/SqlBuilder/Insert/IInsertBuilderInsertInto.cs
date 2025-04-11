namespace InlineSqlSharp;

public interface IInsertBuilderInsertInto : ISqlBuilder
{
    IInsertBuilderSet SET(params IAssignment[] assignments);
}
