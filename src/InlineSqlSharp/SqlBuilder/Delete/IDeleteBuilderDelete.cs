namespace InlineSqlSharp;

public interface IDeleteBuilderDelete : ISqlBuilder
{
	IDeleteBuilderWhere WHERE(ICondition condition);
}
