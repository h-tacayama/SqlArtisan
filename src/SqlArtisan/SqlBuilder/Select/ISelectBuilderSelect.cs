namespace InlineSqlSharp;

public interface ISelectBuilderSelect : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderFrom From(params AbstractTableReference[] tables);
}
