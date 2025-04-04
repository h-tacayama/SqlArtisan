using Dapper;

namespace InlineSqlSharp.Dapper;

internal static class SqlStatementExtensions
{
	internal static DynamicParameters GetDynamicParameters(this SqlStatement sql)
	{
		DynamicParameters parameters = new();

		foreach (BindParameter param in sql.Parameters)
		{
			parameters.Add(param.Name, param.Value, param.DbType, param.Direction);
		}

		return parameters;
	}
}
