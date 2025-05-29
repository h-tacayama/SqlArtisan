using System.Data;

namespace SqlArtisan.Tests;

public class DbmsResolverTest
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
    public void Resolve_SqlConnection_ReturnsSqlite()
    {
        IDbConnection conn = new Microsoft.Data.SqlClient.SqlConnection(); ;
        Dbms dbms = DbmsResolver.Resolve(conn);
        Assert.Equal(Dbms.SqlServer, dbms);
    }
}
