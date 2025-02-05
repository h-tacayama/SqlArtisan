namespace InlineSqlSharp;

public interface ISubqueryBuilder
{
	SqlCommand AsSubquery(int parameterIndex);
}
