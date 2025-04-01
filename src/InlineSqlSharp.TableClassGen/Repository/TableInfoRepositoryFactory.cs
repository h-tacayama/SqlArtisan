namespace InlineSqlSharp.TableClassGen;

internal static class TableInfoRepositoryFactory
{
	public static ITableInfoRepository Create(
		DbConnectionInfo connInfo,
		bool lowercaseNames) =>
		connInfo.DbmsType switch
		{
			DbmsType.Oracle => new OracleTableInfoRepository(connInfo, lowercaseNames),
			DbmsType.PostgreSQL => new PgSqlTableInfoRepository(connInfo, lowercaseNames),
			_ => throw new ArgumentOutOfRangeException(nameof(connInfo.DbmsType))
		};
}
