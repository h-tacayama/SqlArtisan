using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
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

    // MySQL spells a functional index with a doubly-parenthesized expression
    // (8.0.13+), and reports it in STATISTICS.EXPRESSION with a null COLUMN_NAME.
    [Fact]
    public void GenerateTables_MySql_ClaimsLeadingColumnOnly()
    {
        Execute("CREATE INDEX ix_age_dept ON users (age, department_id)");
        Execute("CREATE INDEX ix_upper_name ON users ((upper(name)))");
        try
        {
            InformationSchemaTableInfoRepository repository = new(ConnInfo(), lowercaseNames: false);

            TableClassGenAssertions.AssertCompositeAndExpression(
                repository.GetAllTables(),
                expectedForExpressionColumn: null);
        }
        finally
        {
            Execute("DROP INDEX ix_upper_name ON users");
            Execute("DROP INDEX ix_age_dept ON users");
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        return new DbConnectionInfo(
            DbmsType.MySql,
            builder.Server,
            (int)builder.Port,
            builder.Database,
            builder.Database,
            builder.UserID,
            builder.Password);
    }

    private void Execute(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(sql);
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

    // T-SQL indexes no expression directly; the equivalent is an index whose
    // leading key is a computed column, whose definition names the real column.
    [Fact]
    public void GenerateTables_SqlServer_ClaimsLeadingColumnOnly()
    {
        Execute("CREATE INDEX ix_age_dept ON users (age, department_id)");
        Execute("ALTER TABLE users ADD upper_name AS UPPER(name)");
        Execute("CREATE INDEX ix_upper_name ON users (upper_name)");
        try
        {
            InformationSchemaTableInfoRepository repository = new(ConnInfo(), lowercaseNames: false);

            TableClassGenAssertions.AssertCompositeAndExpression(
                repository.GetAllTables(),
                expectedForExpressionColumn: null);
        }
        finally
        {
            Execute("DROP INDEX ix_upper_name ON users");
            Execute("ALTER TABLE users DROP COLUMN upper_name");
            Execute("DROP INDEX ix_age_dept ON users");
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        SqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Replace("tcp:", string.Empty).Split(',', 2);
        return new DbConnectionInfo(
            DbmsType.SqlServer,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1433,
            string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog,
            "dbo",
            builder.UserID,
            builder.Password);
    }

    private void Execute(string sql)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        connection.Execute(sql);
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

    [Fact]
    public void GenerateTables_PostgreSql_ClaimsLeadingColumnOnly()
    {
        Execute("CREATE INDEX ix_age_dept ON users (age, department_id)");
        Execute("CREATE INDEX ix_upper_name ON users (upper(name))");
        try
        {
            InformationSchemaTableInfoRepository repository = new(ConnInfo(), lowercaseNames: false);

            TableClassGenAssertions.AssertCompositeAndExpression(
                repository.GetAllTables(),
                expectedForExpressionColumn: null);
        }
        finally
        {
            Execute("DROP INDEX IF EXISTS ix_upper_name");
            Execute("DROP INDEX IF EXISTS ix_age_dept");
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

[Trait("Engine", "Oracle")]
public sealed class OracleTableClassGenTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleTableClassGenTests(OracleFixture fixture) => _fixture = fixture;

    // Oracle reads ALL_TAB_COLUMNS rather than information_schema, so its column
    // order and its NULLABLE mapping are only proven here.
    [Fact]
    public void GenerateTables_Oracle_ExtractsSeededSchema()
    {
        OracleConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Split([':', '/'], StringSplitOptions.RemoveEmptyEntries);

        DbConnectionInfo connInfo = new(
            DbmsType.Oracle,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1521,
            dataSource.Length > 2 ? dataSource[2] : "XEPDB1",
            builder.UserID,
            builder.UserID,
            builder.Password);

        OracleTableInfoRepository repository = new(connInfo, lowercaseNames: true);

        TableClassGenAssertions.AssertSeededSchema(
            repository.GetAllTables(),
            expectedHasDefault: false);
    }

    // ALL_IND_EXPRESSIONS.COLUMN_EXPRESSION is a LONG, so the collector never reads
    // it: a function-based index disqualifies every column of the table instead.
    [Fact]
    public void GenerateTables_Oracle_FunctionBasedIndex_ClaimsNothingForTheTable()
    {
        Execute("CREATE INDEX ix_age_dept ON users (age, department_id)");
        try
        {
            OracleTableInfoRepository repository = new(ConnInfo(), lowercaseNames: true);

            TableClassGenAssertions.AssertCompositeAndExpression(
                repository.GetAllTables(),
                expectedForExpressionColumn: false);

            Execute("CREATE INDEX ix_upper_name ON users (upper(name))");

            IReadOnlyList<DbTableInfo> tables =
                new OracleTableInfoRepository(ConnInfo(), lowercaseNames: true).GetAllTables();

            Assert.All(
                tables.Single(t => t.TableName == "users").Columns,
                c => Assert.Null(c.IsIndexed));
        }
        finally
        {
            // Oracle XE has no DROP INDEX IF EXISTS, and an assertion that throws
            // before the second CREATE would otherwise mask itself with ORA-01418.
            TryExecute("DROP INDEX ix_upper_name");
            TryExecute("DROP INDEX ix_age_dept");
        }
    }

    // The four shapes HasDefault has to tell apart. Oracle records the identity
    // sequence and the virtual column's expression in DATA_DEFAULT, so DEFAULT_LENGTH
    // answers all three engine-assigned cases without reading the LONG.
    [Fact]
    public void GenerateTables_Oracle_HasDefault_DistinguishesEngineAssignedColumns()
    {
        Execute(
            """
            CREATE TABLE default_probe (
                plain NUMBER(10),
                defaulted NUMBER(10) DEFAULT 7,
                generated_id NUMBER(10) GENERATED ALWAYS AS IDENTITY,
                virtual_col NUMBER(10) GENERATED ALWAYS AS (plain * 2))
            """);
        try
        {
            DbTableInfo table = new OracleTableInfoRepository(ConnInfo(), lowercaseNames: true)
                .GetAllTables()
                .Single(t => t.TableName == "default_probe");

            Assert.Equal(
                ["plain", "defaulted", "generated_id", "virtual_col"],
                table.Columns.Select(c => c.Name));
            Assert.Equal([false, true, true, true], table.Columns.Select(c => c.HasDefault));
        }
        finally
        {
            TryExecute("DROP TABLE default_probe");
        }
    }

    private void TryExecute(string sql)
    {
        try
        {
            Execute(sql);
        }
        catch (OracleException)
        {
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        OracleConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Split([':', '/'], StringSplitOptions.RemoveEmptyEntries);

        return new DbConnectionInfo(
            DbmsType.Oracle,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1521,
            dataSource.Length > 2 ? dataSource[2] : "XEPDB1",
            builder.UserID,
            builder.UserID,
            builder.Password);
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
    public static void AssertSeededSchema(
        IReadOnlyList<DbTableInfo> tables,
        bool? expectedHasDefault = null)
    {
        DbTableInfo users = Find(tables, "users");
        Assert.Equal(
            ["id", "name", "age", "department_id", "created_at", "is_active", "data"],
            users.Columns.Select(c => c.Name.ToLowerInvariant()));
        Assert.Equal("UsersTable", users.ClassName);

        // The metadata #266 reasons over, proven per engine rather than assumed:
        // the primary key is NOT NULL on every engine, and a plain column is not.
        Assert.False(Column(users, "id").IsNullable);
        Assert.True(Column(users, "name").IsNullable);

        // No column of the seeded schema has a DEFAULT. On information_schema that
        // reads as unknown — an identity column reports none either — while Oracle
        // can tell the two apart and answers false.
        Assert.All(users.Columns, c => Assert.Equal(expectedHasDefault, c.HasDefault));

        // The primary key is indexed on every engine, and the seeded schema has no
        // other index — so this proves each engine's leading-key query both ways.
        Assert.True(Column(users, "id").IsIndexed);
        Assert.False(Column(users, "name").IsIndexed);

        DbTableInfo orders = Find(tables, "orders");
        Assert.Equal(
            ["id", "user_id", "amount"],
            orders.Columns.Select(c => c.Name.ToLowerInvariant()));
    }

    // The two collection boundaries #266 requires proving live: only the leading
    // column of a composite index is claimed, and a column an index expression
    // names is claimed either way.
    public static void AssertCompositeAndExpression(
        IReadOnlyList<DbTableInfo> tables,
        bool? expectedForExpressionColumn)
    {
        DbTableInfo users = Find(tables, "users");

        Assert.True(Column(users, "age").IsIndexed);
        Assert.False(Column(users, "department_id").IsIndexed);
        Assert.Equal(expectedForExpressionColumn, Column(users, "name").IsIndexed);
    }

    private static DbTableInfo Find(IReadOnlyList<DbTableInfo> tables, string name) =>
        tables.Single(t => string.Equals(t.TableName, name, StringComparison.OrdinalIgnoreCase));

    private static DbColumnInfo Column(DbTableInfo table, string name) =>
        table.Columns.Single(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}
