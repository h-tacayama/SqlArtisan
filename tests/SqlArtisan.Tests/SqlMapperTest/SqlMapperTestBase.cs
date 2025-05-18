using System.Data;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.DapperExtensions;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

[CollectionDefinition("SequentialTests")]
public class SequentialCollectionDefinition
{
    // This class serves as a marker and holds collection settings.
}

public abstract class SqlMapperTestBase : IDisposable
{
    protected readonly IDbConnection _conn;
    protected readonly string _connString = "User Id=xxx;Password=xxx;Data Source=xxx:1521/xxx";

    protected SqlMapperTestBase()
    {
        _conn = new OracleConnection(_connString);
        _conn.Open();

        SetUpTestTable();
    }

    private void SetUpTestTable()
    {
        if (IsTestTableExists())
        {
            throw new Exception("The test table already exists.");
        }

        using IDbCommand createCommand = _conn.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE test_table (
                code NUMBER(7,0) PRIMARY KEY,
                name VARCHAR2(100),
                created_at DATE
            )
            TABLESPACE USERS";
        createCommand.ExecuteNonQuery();

        try
        {
            InsertTestData(1, "Test1", new DateTime(2025, 4, 1));
            InsertTestData(2, "Test2", new DateTime(2025, 4, 2));
        }
        catch { }
    }

    public void Dispose()
    {
        using IDbCommand command = _conn.CreateCommand();
        command.CommandText = "DROP TABLE test_table";
        command.ExecuteNonQuery();

        _conn?.Dispose();
    }

    private bool IsTestTableExists()
    {
        try
        {
            TestTable t = new("t");
            ISqlBuilder sql = Select(Count(t.Code)).From(t);
            _conn.ExecuteScalar<int>(sql);
            return true;
        }
        catch (OracleException ex)
        {
            return !ex.Message.Contains("ORA-00942");
        }
    }

    private void InsertTestData(int code, string name, DateTime createdAt)
    {
        TestTable t = new();

        ISqlBuilder sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Values(code, name, ToDate(createdAt.ToString("yyyy/MM/dd"), "yyyy/mm/dd"));

        int rows = _conn.Execute(sql);
    }
}
