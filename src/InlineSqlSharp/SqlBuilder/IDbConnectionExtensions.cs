using System.Data;

namespace InlineSqlSharp;

public static class IDbConnectionExtensions
{
	public static IDbCommand CreateCommand(
		this IDbConnection connection,
		ISqlBuilder sqlBuilder)
	{
		SqlCommand sql = sqlBuilder.Build();

		IDbCommand command = connection.CreateCommand();
		command.CommandText = sql.Statement;

		foreach (BindParameter bindVar in sql.Parameters)
		{
			IDbDataParameter parameter = command.CreateParameter();
			parameter.ParameterName = bindVar.Name;
			parameter.Value = bindVar.Value;
			parameter.Direction = bindVar.Direction;
			command.Parameters.Add(parameter);
		}

		return command;
	}
}
