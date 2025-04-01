using System.Data;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace InlineSqlSharp.TableClassGen;

internal sealed class DbConnectionInfo(
	DbmsType databaseType,
	string host,
	int port,
	string serviceName,
	string schema,
	string username,
	string password)
{
	public DbmsType DbmsType => databaseType;

	public string Host => host;

	public int Port => port;

	public string ServiceName => serviceName;

	public string Schema => schema;

	public string Username => username;

	public string Password => password;

	public IDbConnection CreateConnection() =>
		DbmsType switch
		{
			DbmsType.Oracle => new OracleConnection(GetConnectionString()),
			DbmsType.PostgreSQL => new NpgsqlConnection(GetConnectionString()),
			_ => throw new ArgumentOutOfRangeException(nameof(DbmsType))
		};

	private string GetConnectionString() =>
		DbmsType switch
		{
			DbmsType.Oracle =>
				$"User Id={Username};Password={Password};Data Source={Host}:{Port}/{ServiceName}",
			DbmsType.PostgreSQL =>
				$"Host={Host};Port={Port};Database={ServiceName};Username={Username};Password={Password}",
			_ => throw new ArgumentOutOfRangeException(nameof(DbmsType))
		};
}
