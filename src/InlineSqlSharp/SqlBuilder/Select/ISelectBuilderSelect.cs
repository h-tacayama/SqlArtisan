namespace InlineSqlSharp;

public interface ISelectBuilderSelect : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderFrom FROM(params AbstractTableReference[] tables);
}
