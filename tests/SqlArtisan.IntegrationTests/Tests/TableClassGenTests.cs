using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.IntegrationTests.Tests;

// Verifies the TableClassGen repositories against live engines: the SqlArtisan
// information_schema builder path resolves the right dialect from the connection
// and extracts the seeded schema. SQLite's bespoke path is covered in the fast
// unit lane (SqlArtisan.TableClassGen.Tests).

[Trait("Engine", "MySql")]
public sealed class MySqlTableClassGenTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlTableClassGenTests(MySqlFixture fixture) => _fixture = fixture;

    [Fact]
    public void GenerateTables_MySql_ExtractsSeededSchema()
    {
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        DbConnectionInfo connInfo = new(
            DbmsType.MySql,
            builder.Server,
            (int)builder.Port,
            builder.Database,
            builder.Database,
            builder.UserID,
            builder.Password);

        InformationSchemaTableInfoRepository repository = new(connInfo, lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(repository.GetAllTables());
    }
}

[Trait("Engine", "SqlServer")]
public sealed class SqlServerTableClassGenTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerTableClassGenTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public void GenerateTables_SqlServer_ExtractsSeededSchema()
    {
        SqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Replace("tcp:", string.Empty).Split(',', 2);
        DbConnectionInfo connInfo = new(
            DbmsType.SqlServer,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1433,
            string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog,
            "dbo",
            builder.UserID,
            builder.Password);

        InformationSchemaTableInfoRepository repository = new(connInfo, lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(repository.GetAllTables());
    }
}

[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlTableClassGenTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlTableClassGenTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public void GenerateTables_PostgreSql_ExtractsSeededSchema()
    {
        InformationSchemaTableInfoRepository repository = new(ConnInfo(), lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(repository.GetAllTables());
    }

    // #323: PostgreSQL's information_schema comparison is case-sensitive, so a
    // mixed-case table must survive lowercaseNames — the fix keeps the catalog's
    // stored name as the re-lookup key and lowercases only the emitted name.
    [Fact]
    public void GenerateTables_PostgreSql_LowercaseNames_KeepsMixedCaseTable()
    {
        Execute("CREATE TABLE IF NOT EXISTS \"MixedCaseTbl\" (\"Id\" integer, \"Val\" varchar(10))");
        try
        {
            InformationSchemaTableInfoRepository repository = new(ConnInfo(), lowercaseNames: true);

            IReadOnlyList<DbTableInfo> tables = repository.GetAllTables();

            Assert.Contains(tables, t => t.TableName == "mixedcasetbl");
        }
        finally
        {
            Execute("DROP TABLE IF EXISTS \"MixedCaseTbl\"");
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        NpgsqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        return new DbConnectionInfo(
            DbmsType.PostgreSql,
            builder.Host!,
            builder.Port,
            builder.Database!,
            "public",
            builder.Username!,
            builder.Password!);
    }

    private void Execute(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(sql);
    }
}

internal static class TableClassGenAssertions
{
    // The seeded schema (TestSchema): `users` and `orders`. SQL Server's master db
    // also carries system base tables, so assert presence, not an exact table set.
    public static void AssertSeededSchema(IReadOnlyList<DbTableInfo> tables)
    {
        DbTableInfo users = Find(tables, "users");
        Assert.Equal(
            ["id", "name", "age", "department_id", "created_at", "is_active", "data"],
            users.Columns.Select(c => c.Name.ToLowerInvariant()));
        Assert.Equal("UsersTable", users.ClassName);

        DbTableInfo orders = Find(tables, "orders");
        Assert.Equal(
            ["id", "user_id", "amount"],
            orders.Columns.Select(c => c.Name.ToLowerInvariant()));
    }

    private static DbTableInfo Find(IReadOnlyList<DbTableInfo> tables, string name) =>
        tables.Single(t => string.Equals(t.TableName, name, StringComparison.OrdinalIgnoreCase));
}
