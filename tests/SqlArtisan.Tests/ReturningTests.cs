using System.Data;
using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ReturningTests
{
    private readonly TestTable _t = new();

    // ── plain RETURNING ────────────────────────────────────────────────

    [Fact]
    public void Returning_OnInsertWithValues_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnInsertWithSet_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Asterisk_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(Asterisk)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING *");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnDelete_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnDeleteWithWhere_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE code = :0 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnUpdate_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnUpdateWithWhere_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Where(_t.Code > 0)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("WHERE code > :2 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── RETURNING INTO ────────────────────────────────────────────────

    [Fact]
    public void ReturningInto_OnDelete_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnDeleteWithWhere_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE code = :0 ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnInsert_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnUpdate_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── validation ────────────────────────────────────────────────────

    [Fact]
    public void Returning_NoArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning([])
            .Build());
    }

    [Fact]
    public void Returning_WithExpressionAlias_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code.As("b"))
            .Build());
    }

    [Fact]
    public void ReturningInto_NoArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into());
    }

    [Fact]
    public void ReturningInto_VariableCountMismatch_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new OutputParameter("b", DbType.Int32))
            .Build());
    }

    [Fact]
    public void ReturningInto_DuplicateVariableName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("b", DbType.Int32))
            .Build());
    }

    [Fact]
    public void OutputParameter_EmptyVariable_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new OutputParameter("", DbType.Int32));
    }

    // ── output parameters ─────────────────────────────────────────────

    [Fact]
    public void ReturningInto_OnDelete_RegistersOutputParameters()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        Dictionary<string, BindValue> parameters = new();
        sql.Parameters.ForEach((name, bind) => parameters.Add(name, bind));

        Assert.Equal(ParameterDirection.Output, parameters[":b"].Direction);
        Assert.Equal(DbType.Int32, parameters[":b"].DbType);
        Assert.Equal(ParameterDirection.Output, parameters[":c"].Direction);
        Assert.Equal(DbType.String, parameters[":c"].DbType);
        Assert.Equal(100, parameters[":c"].Size);
    }

    [Fact]
    public void ReturningInto_OnSqlServer_UsesDialectParameterMarker()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.SqlServer);

        Assert.Contains("INTO @b, @c", sql.Text);
        Assert.Contains("@b", sql.Parameters.ParameterNames);
        Assert.Contains("@c", sql.Parameters.ParameterNames);
    }
}
