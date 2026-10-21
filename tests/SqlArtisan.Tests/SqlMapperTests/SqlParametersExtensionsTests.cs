using System.Data;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class SqlParametersExtensionsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToDynamicParameters_BindNull_BindsSqlNull(bool typed)
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        global::Dapper.SqlMapper.Execute(connection, "CREATE TABLE t (id INTEGER, name TEXT)");
        DbTable t = new("t");
        DbColumn id = new(t, "id");
        DbColumn name = new(t, "name");
        BindValue Null() => typed ? BindNull(DbType.String) : BindNull();

        connection.Execute(InsertInto(t, id, name).Values(1, Null()));
        SqlStatement handOff = InsertInto(t, id, name).Values(2, Null()).Build(Dbms.Sqlite);
        global::Dapper.SqlMapper.Execute(
            connection, handOff.Text, handOff.Parameters.ToDynamicParameters());

        Assert.Equal(
            2L,
            global::Dapper.SqlMapper.ExecuteScalar<long>(
                connection, "SELECT COUNT(*) FROM t WHERE name IS NULL"));
    }
}
