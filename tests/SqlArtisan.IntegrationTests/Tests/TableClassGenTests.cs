using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.IntegrationTests.Tests;

// Verifies the TableClassGen catalog readers against live engines: the SqlArtisan
// information_schema builder path resolves the right dialect from the connection
// and extracts the seeded schema. SQLite's bespoke path is covered in the fast
// unit lane (SqlArtisan.TableClassGen.Tests).

[Trait("Engine", "MySql")]
public sealed class MySqlTableClassGenTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlTableClassGenTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GenerateTables_MySql_ExtractsSeededSchema()
    {
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        DbConnectionInfo connInfo = new(
            Dbms.MySql,
            builder.Server,
            (int)builder.Port,
            builder.Database,
            builder.Database,
            builder.UserID,
            builder.Password);

        InformationSchemaCatalogReader reader = new(connInfo, lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(reader.GetAllTables());
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
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

            TableClassGenAssertions.AssertCompositeAndExpression(
                reader.GetAllTables(),
                expectedForExpressionColumn: null);
        }
        finally
        {
            Execute("DROP INDEX ix_upper_name ON users");
            Execute("DROP INDEX ix_age_dept ON users");
        }
    }

    // #386: the fixture's own connecting user has grants on the seeded database
    // only (Testcontainers' default MySQL account carries no CREATE USER, so a
    // restricted second account cannot be minted here) — that user still
    // authenticates against the real "mysql" system schema, but information_schema
    // shows none of its tables, exactly as it shows none for a schema that does
    // not exist at all.
    [Fact]
    public void GenerateTables_MySql_NoPrivileges_ReturnsEmptyLikeAnUnknownSchema()
    {
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString);

        IReadOnlyList<CatalogTable> realSchemaNoGrant = SchemaReader(builder, "mysql").GetAllTables();
        IReadOnlyList<CatalogTable> unknownSchema =
            SchemaReader(builder, "sqlartisan_unknown_schema").GetAllTables();

        Assert.Empty(realSchemaNoGrant);
        Assert.Empty(unknownSchema);
    }

    // The connection string's Database (what USE selects) stays the seeded,
    // granted database; only the schema the WHERE clause filters by changes —
    // so this never touches the CREATE-USER-requiring connect-time path.
    private static InformationSchemaCatalogReader SchemaReader(
        MySqlConnectionStringBuilder builder, string schema) =>
        new(
            new DbConnectionInfo(
                Dbms.MySql,
                builder.Server,
                (int)builder.Port,
                builder.Database,
                schema,
                builder.UserID,
                builder.Password),
            lowercaseNames: false);

    private DbConnectionInfo ConnInfo()
    {
        MySqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        return new DbConnectionInfo(
            Dbms.MySql,
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

    public SqlServerTableClassGenTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GenerateTables_SqlServer_ExtractsSeededSchema()
    {
        SqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Replace("tcp:", string.Empty).Split(',', 2);
        DbConnectionInfo connInfo = new(
            Dbms.SqlServer,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1433,
            string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog,
            "dbo",
            builder.UserID,
            builder.Password);

        InformationSchemaCatalogReader reader = new(connInfo, lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(reader.GetAllTables());
    }

    // T-SQL indexes no expression directly; the equivalent is an index whose
    // leading key is a computed column, whose definition names the real column.
    [Fact]
    public void GenerateTables_SqlServer_ClaimsLeadingColumnOnly()
    {
        Execute("CREATE INDEX ix_age_dept ON users (age, department_id)");
        Execute("ALTER TABLE users ADD upper_name AS UPPER(name)");
        Execute("CREATE INDEX ix_upper_name ON users (upper_name)");
        Execute("CREATE INDEX ix_filtered ON users (is_active) WHERE age > 0");
        try
        {
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

            IReadOnlyList<CatalogTable> tables = reader.GetAllTables();
            TableClassGenAssertions.AssertCompositeAndExpression(
                tables,
                expectedForExpressionColumn: null);

            CatalogTable users = tables.Single(
                t => string.Equals(t.TableName, "users", StringComparison.OrdinalIgnoreCase));
            // The computed column leads its index, so it is claimed even though the
            // same row carries the definition that suppresses the real column.
            Assert.True(users.Columns.Single(c => c.Name == "upper_name").IsIndexed);
            Assert.Null(users.Columns.Single(c => c.Name == "is_active").IsIndexed);
        }
        finally
        {
            Execute("DROP INDEX ix_filtered ON users");
            Execute("DROP INDEX ix_upper_name ON users");
            Execute("ALTER TABLE users DROP COLUMN upper_name");
            Execute("DROP INDEX ix_age_dept ON users");
        }
    }

    // A disabled index serves no query, so it must not claim its column.
    [Fact]
    public void GenerateTables_SqlServer_DisabledIndex_ClaimsNothing()
    {
        Execute("CREATE INDEX ix_disabled ON users (age)");
        Execute("ALTER INDEX ix_disabled ON users DISABLE");
        try
        {
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

            CatalogTable users = reader.GetAllTables().Single(
                t => string.Equals(t.TableName, "users", StringComparison.OrdinalIgnoreCase));
            Assert.Null(users.Columns.Single(c => c.Name == "age").IsIndexed);
        }
        finally
        {
            Execute("DROP INDEX ix_disabled ON users");
        }
    }

    // #386: a login mapped to a user with no grants still connects — CONNECT
    // comes via the public role. The read never throws, which is the property
    // in question, but it is not empty: master carries legacy compatibility
    // tables (spt_fallback_db and siblings) SQL Server grants to public by
    // default, so those are the only rows visible — proving the filtering,
    // not a bare empty result.
    [Fact]
    public void GenerateTables_SqlServer_NoPrivileges_FiltersRatherThanThrows()
    {
        Execute("CREATE LOGIN sqlartisan_restricted WITH PASSWORD = 'Restricted-Pw1!'");
        Execute("CREATE USER sqlartisan_restricted FOR LOGIN sqlartisan_restricted");
        try
        {
            SqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
            string[] dataSource = builder.DataSource.Replace("tcp:", string.Empty).Split(',', 2);
            DbConnectionInfo connInfo = new(
                Dbms.SqlServer,
                dataSource[0],
                dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1433,
                string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog,
                "dbo",
                "sqlartisan_restricted",
                "Restricted-Pw1!");

            InformationSchemaCatalogReader reader = new(connInfo, lowercaseNames: false);

            IReadOnlyList<CatalogTable> tables = reader.GetAllTables();

            Assert.NotEmpty(tables);
            Assert.DoesNotContain(tables, t => t.TableName == "users" || t.TableName == "orders");
        }
        finally
        {
            // The reader's connection is disposed, but ADO.NET pooling keeps the
            // underlying session alive server-side — DROP LOGIN fails against a
            // login that still looks logged in without this.
            SqlConnection.ClearAllPools();
            Execute("DROP USER sqlartisan_restricted");
            Execute("DROP LOGIN sqlartisan_restricted");
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        SqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Replace("tcp:", string.Empty).Split(',', 2);
        return new DbConnectionInfo(
            Dbms.SqlServer,
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

    public PostgreSqlTableClassGenTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GenerateTables_PostgreSql_ExtractsSeededSchema()
    {
        InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

        TableClassGenAssertions.AssertSeededSchema(reader.GetAllTables());
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
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: true);

            IReadOnlyList<CatalogTable> tables = reader.GetAllTables();

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
        // The mixed row: pg_index reports indkey[0] and the expression list in ONE
        // row, so created_at must survive as a claimed lead beside the expression.
        Execute("CREATE INDEX ix_mixed ON users (created_at, lower(name))");
        Execute("CREATE INDEX ix_partial ON users (is_active) WHERE age > 0");
        try
        {
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

            IReadOnlyList<CatalogTable> tables = reader.GetAllTables();
            TableClassGenAssertions.AssertCompositeAndExpression(
                tables,
                expectedForExpressionColumn: null);

            CatalogTable users = tables.Single(t => t.TableName == "users");
            Assert.True(users.Columns.Single(c => c.Name == "created_at").IsIndexed);
            Assert.Null(users.Columns.Single(c => c.Name == "is_active").IsIndexed);
        }
        finally
        {
            Execute("DROP INDEX IF EXISTS ix_partial");
            Execute("DROP INDEX IF EXISTS ix_mixed");
            Execute("DROP INDEX IF EXISTS ix_upper_name");
            Execute("DROP INDEX IF EXISTS ix_age_dept");
        }
    }

    // An invalid index (a failed CONCURRENTLY build's end state) serves no
    // query, so it must not claim its column. The flag is flipped directly —
    // a real failed concurrent build is nondeterministic to stage.
    [Fact]
    public void GenerateTables_PostgreSql_InvalidIndex_ClaimsNothing()
    {
        Execute("CREATE INDEX ix_invalid ON users (age)");
        Execute(
            "UPDATE pg_index SET indisvalid = false "
                + "WHERE indexrelid = 'ix_invalid'::regclass");
        try
        {
            InformationSchemaCatalogReader reader = new(ConnInfo(), lowercaseNames: false);

            CatalogTable users = reader.GetAllTables().Single(t => t.TableName == "users");
            Assert.Null(users.Columns.Single(c => c.Name == "age").IsIndexed);
        }
        finally
        {
            Execute("DROP INDEX IF EXISTS ix_invalid");
        }
    }

    // #386: a fresh role connects fine — CONNECT is PUBLIC-granted by default —
    // but sees none of the seeded tables, the same empty catalog an unknown
    // --schema produces.
    [Fact]
    public void GenerateTables_PostgreSql_NoPrivileges_ReturnsEmptyLikeAnUnknownSchema()
    {
        Execute("CREATE ROLE sqlartisan_restricted LOGIN PASSWORD 'Restricted-Pw1!'");
        try
        {
            NpgsqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
            DbConnectionInfo connInfo = new(
                Dbms.PostgreSql,
                builder.Host!,
                builder.Port,
                builder.Database!,
                "public",
                "sqlartisan_restricted",
                "Restricted-Pw1!");

            InformationSchemaCatalogReader reader = new(connInfo, lowercaseNames: false);

            Assert.Empty(reader.GetAllTables());
        }
        finally
        {
            Execute("DROP ROLE sqlartisan_restricted");
        }
    }

    private DbConnectionInfo ConnInfo()
    {
        NpgsqlConnectionStringBuilder builder = new(_fixture.ConnectionString);
        return new DbConnectionInfo(
            Dbms.PostgreSql,
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

    public OracleTableClassGenTests(OracleFixture fixture)
    {
        _fixture = fixture;
    }

    // Oracle reads ALL_TAB_COLUMNS rather than information_schema, so its column
    // order and its NULLABLE mapping are only proven here.
    [Fact]
    public void GenerateTables_Oracle_ExtractsSeededSchema()
    {
        OracleConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Split([':', '/'], StringSplitOptions.RemoveEmptyEntries);

        DbConnectionInfo connInfo = new(
            Dbms.Oracle,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1521,
            dataSource.Length > 2 ? dataSource[2] : "XEPDB1",
            builder.UserID,
            builder.UserID,
            builder.Password);

        OracleCatalogReader reader = new(connInfo, lowercaseNames: true);

        TableClassGenAssertions.AssertSeededSchema(
            reader.GetAllTables(),
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
            OracleCatalogReader reader = new(ConnInfo(), lowercaseNames: true);

            TableClassGenAssertions.AssertCompositeAndExpression(
                reader.GetAllTables(),
                expectedForExpressionColumn: false);

            Execute("CREATE INDEX ix_upper_name ON users (upper(name))");

            IReadOnlyList<CatalogTable> tables =
                new OracleCatalogReader(ConnInfo(), lowercaseNames: true).GetAllTables();

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

    // An UNUSABLE index serves no query, so it must not claim its column
    // (ALL_IND_COLUMNS records no status; the ALL_INDEXES join supplies it).
    [Fact]
    public void GenerateTables_Oracle_UnusableIndex_ClaimsNothing()
    {
        Execute("CREATE INDEX ix_unusable ON users (age)");
        Execute("ALTER INDEX ix_unusable UNUSABLE");
        try
        {
            OracleCatalogReader reader = new(ConnInfo(), lowercaseNames: true);

            CatalogTable users = reader.GetAllTables().Single(t => t.TableName == "users");
            Assert.Null(users.Columns.Single(c => c.Name == "age").IsIndexed);
        }
        finally
        {
            TryExecute("DROP INDEX ix_unusable");
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
            CatalogTable table = new OracleCatalogReader(ConnInfo(), lowercaseNames: true)
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

    // #386: ALL_TABLES is privilege-filtered like the other engines' catalogs —
    // reading it never throws for a schema the app user has no grant into. It is
    // not empty here: SYSTEM owns a handful of tables (HELP, OL$...) Oracle grants
    // to PUBLIC by default, so those are the only rows visible — proving the
    // filtering, not a bare empty result. No restricted user is needed: the app
    // user's own CONNECT + RESOURCE grant already excludes it from everything else
    // SYSTEM owns.
    [Fact]
    public void GenerateTables_Oracle_NoPrivileges_FiltersRatherThanThrows()
    {
        OracleConnectionStringBuilder builder = new(_fixture.ConnectionString);
        string[] dataSource = builder.DataSource.Split([':', '/'], StringSplitOptions.RemoveEmptyEntries);

        DbConnectionInfo connInfo = new(
            Dbms.Oracle,
            dataSource[0],
            dataSource.Length > 1 ? int.Parse(dataSource[1]) : 1521,
            dataSource.Length > 2 ? dataSource[2] : "XEPDB1",
            "SYSTEM",
            builder.UserID,
            builder.Password);

        OracleCatalogReader reader = new(connInfo, lowercaseNames: true);

        IReadOnlyList<CatalogTable> tables = reader.GetAllTables();

        Assert.NotEmpty(tables);
        Assert.DoesNotContain(tables, t => t.TableName == "users" || t.TableName == "orders");
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
            Dbms.Oracle,
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
        IReadOnlyList<CatalogTable> tables,
        bool? expectedHasDefault = null)
    {
        CatalogTable users = Find(tables, "users");
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

        CatalogTable orders = Find(tables, "orders");
        Assert.Equal(
            ["id", "user_id", "amount"],
            orders.Columns.Select(c => c.Name.ToLowerInvariant()));
    }

    // The two collection boundaries #266 requires proving live: only the leading
    // column of a composite index is claimed, and a column an index expression
    // names is claimed either way.
    public static void AssertCompositeAndExpression(
        IReadOnlyList<CatalogTable> tables,
        bool? expectedForExpressionColumn)
    {
        CatalogTable users = Find(tables, "users");

        Assert.True(Column(users, "age").IsIndexed);
        Assert.False(Column(users, "department_id").IsIndexed);
        Assert.Equal(expectedForExpressionColumn, Column(users, "name").IsIndexed);
    }

    private static CatalogTable Find(IReadOnlyList<CatalogTable> tables, string name) =>
        tables.Single(t => string.Equals(t.TableName, name, StringComparison.OrdinalIgnoreCase));

    private static CatalogColumn Column(CatalogTable table, string name) =>
        table.Columns.Single(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}
