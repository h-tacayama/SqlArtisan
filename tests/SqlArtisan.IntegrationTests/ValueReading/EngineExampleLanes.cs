using System.Data;
using Npgsql;
using SqlArtisan.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SqlArtisan.IntegrationTests.ValueReading;

[Collection("ValueReading")]
[Trait("Engine", "Sqlite")]
public sealed class SqliteValueReadingTests : EngineExampleTestBase, IClassFixture<SqliteFixture>
{
    public SqliteValueReadingTests(SqliteFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Sqlite, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlValueReadingTests
    : EngineExampleTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlValueReadingTests(PostgreSqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17ValueReadingTests
    : EngineExampleTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17ValueReadingTests(PostgreSql17Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql17, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "MySql")]
public sealed class MySqlValueReadingTests : EngineExampleTestBase, IClassFixture<MySqlFixture>
{
    public MySqlValueReadingTests(MySqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.MySql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer")]
public sealed class SqlServerValueReadingTests
    : EngineExampleTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerValueReadingTests(SqlServerFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025ValueReadingTests
    : EngineExampleTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025ValueReadingTests(SqlServer2025Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer2025, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle")]
public sealed class OracleValueReadingTests : EngineExampleTestBase, IClassFixture<OracleFixture>
{
    public OracleValueReadingTests(OracleFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiValueReadingTests
    : EngineExampleTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiValueReadingTests(Oracle23aiFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle23ai, o)
    {
    }
}

/// <summary>Trial-only: the container's own PostgreSQL 16, reached through
/// VR_PG_CONNECTION, since this session has no Docker daemon for Testcontainers.</summary>
[Collection("ValueReading")]
[Trait("Engine", "LocalPostgreSql")]
public sealed class LocalPostgreSqlValueReadingTests : EngineExampleTestBase
{
    public LocalPostgreSqlValueReadingTests(ITestOutputHelper o)
        : base(Open, Lane.PostgreSql, o)
    {
    }

    internal static IDbConnection Open()
    {
        NpgsqlConnection cnn = new(Environment.GetEnvironmentVariable("VR_PG_CONNECTION")
            ?? throw new InvalidOperationException("Set VR_PG_CONNECTION."));
        cnn.Open();
        return cnn;
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Sqlite")]
public sealed class SqliteValuePositionTests
    : ValuePositionTestBase, IClassFixture<SqliteFixture>
{
    public SqliteValuePositionTests(SqliteFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Sqlite, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlValuePositionTests
    : ValuePositionTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlValuePositionTests(PostgreSqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17ValuePositionTests
    : ValuePositionTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17ValuePositionTests(PostgreSql17Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql17, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "MySql")]
public sealed class MySqlValuePositionTests
    : ValuePositionTestBase, IClassFixture<MySqlFixture>
{
    public MySqlValuePositionTests(MySqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.MySql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer")]
public sealed class SqlServerValuePositionTests
    : ValuePositionTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerValuePositionTests(SqlServerFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025ValuePositionTests
    : ValuePositionTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025ValuePositionTests(SqlServer2025Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer2025, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle")]
public sealed class OracleValuePositionTests
    : ValuePositionTestBase, IClassFixture<OracleFixture>
{
    public OracleValuePositionTests(OracleFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiValuePositionTests
    : ValuePositionTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiValuePositionTests(Oracle23aiFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle23ai, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "LocalPostgreSql")]
public sealed class LocalPostgreSqlValuePositionTests : ValuePositionTestBase
{
    public LocalPostgreSqlValuePositionTests(ITestOutputHelper o)
        : base(LocalPostgreSqlValueReadingTests.Open, Lane.PostgreSql, o)
    {
    }
}
