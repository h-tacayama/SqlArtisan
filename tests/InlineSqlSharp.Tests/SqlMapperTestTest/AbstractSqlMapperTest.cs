using System.Data;
using InlineSqlSharp.Dapper;
using Oracle.ManagedDataAccess.Client;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public abstract class AbstractSqlMapperTest : IDisposable
{
    protected readonly IDbConnection _conn;
    protected readonly string _connString = "User Id=xxx;Password=xxx;Data Source=xxx:1521/xxx";

    protected AbstractSqlMapperTest()
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
            TABLESPACE TS_TBL_02";
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
            test_table t = new("t");
            ISqlBuilder sql = SELECT(COUNT(t.code)).FROM(t);
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
        test_table t = new("t");

        ISqlBuilder sql =
            INSERT_INTO(t)
            .SET(
                t.code == P(code),
                t.name == P(name),
                t.created_at == TO_DATE(L(createdAt.ToString("yyyy/MM/dd")), L("yyyy/mm/dd")));

        int rows = _conn.Execute(sql);
    }
}
