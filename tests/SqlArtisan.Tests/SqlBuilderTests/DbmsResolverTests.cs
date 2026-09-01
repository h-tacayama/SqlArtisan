using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SqlArtisan.Tests;

public class DbmsResolverTests
{
    [Fact]
    public void Resolve_MySqlConnectorConnection_ReturnsMySql()
    {
        IDbConnection conn = new MySqlConnector.MySqlConnection();
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.MySql, dbms);
    }

    [Fact]
    public void Resolve_MySqlDataConnection_ReturnsMySql()
    {
        IDbConnection conne = new MySql.Data.MySqlClient.MySqlConnection();
        Dbms dbms = DbmsResolver.Resolve(conne);
        Assert.Equal(Dbms.MySql, dbms);
    }

    [Fact]
    public void Resolve_OracleConnection_ReturnsOracle()
    {
        IDbConnection conn = new Oracle.ManagedDataAccess.Client.OracleConnection();
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.Oracle, dbms);
    }

    [Fact]
    public void Resolve_NpgsqlConnection_ReturnsPostgreSql()
    {
        IDbConnection conn = new Npgsql.NpgsqlConnection();
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.PostgreSql, dbms);
    }

    [Fact]
    public void Resolve_SqliteConnection_ReturnsSqlite()
    {
        IDbConnection conn = new Microsoft.Data.Sqlite.SqliteConnection();
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.Sqlite, dbms);
    }

    [Fact]
    public void Resolve_SqlConnection_ReturnsSqlServer()
    {
        IDbConnection conn = new Microsoft.Data.SqlClient.SqlConnection();
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.SqlServer, dbms);
    }

    [Fact]
    public void Resolve_NullConnection_ReturnsUnknown() =>
        Assert.Equal(Dbms.Unknown, DbmsResolver.Resolve(null!));

    [Fact]
    public void Resolve_UnregisteredConnectionType_ReturnsUnknown() =>
        Assert.Equal(Dbms.Unknown, DbmsResolver.Resolve(new UnregisteredConnection()));

    [Fact]
    public void RegisterProvider_CustomConnectionType_ResolvesToRegisteredDbms()
    {
        DbmsResolver.RegisterProvider(typeof(RegisteredConnection).FullName!, Dbms.PostgreSql);

        Assert.Equal(Dbms.PostgreSql, DbmsResolver.Resolve(new RegisteredConnection()));
    }

    // The nine built-ins register in the static constructor, before any user
    // code runs, so this documented policy makes them un-overridable.
    [Fact]
    public void RegisterProvider_BuiltInTypeAgain_FirstRegistrationWins()
    {
        DbmsResolver.RegisterProvider("Npgsql.NpgsqlConnection", Dbms.MySql);

        Assert.Equal(Dbms.PostgreSql, DbmsResolver.Resolve(new Npgsql.NpgsqlConnection()));
    }

    [Fact]
    public void RegisterProvider_WhiteSpaceTypeName_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => DbmsResolver.RegisterProvider(" ", Dbms.MySql));

    private sealed class RegisteredConnection : FakeConnectionBase;

    private sealed class UnregisteredConnection : FakeConnectionBase;

    // Only the runtime type matters to Resolve; every member is inert.
    private abstract class FakeConnectionBase : IDbConnection
    {
        [AllowNull]
        public string ConnectionString { get; set; } = string.Empty;

        public int ConnectionTimeout => 0;

        public string Database => string.Empty;

        public ConnectionState State => ConnectionState.Closed;

        public IDbTransaction BeginTransaction() => throw new NotSupportedException();

        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();

        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public void Close()
        {
        }

        public IDbCommand CreateCommand() => throw new NotSupportedException();

        public void Dispose()
        {
        }

        public void Open() => throw new NotSupportedException();
    }
}
