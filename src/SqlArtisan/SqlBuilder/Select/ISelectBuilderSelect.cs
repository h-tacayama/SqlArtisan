namespace SqlArtisan;

public interface ISelectBuilderSelect : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderFrom From(params TableReference[] tables);
}
