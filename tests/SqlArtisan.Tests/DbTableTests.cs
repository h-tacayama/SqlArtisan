using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class DbTableTests
{
    [Fact]
    public void DbTable_Aliased_CorrectSql()
    {
        DbTable u = new("users", "u");

        SqlStatement sql =
            Select(u.Column("id"), u.Column("name"))
            .From(u)
            .Where(u.Column("id") > 0)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"u\".id, \"u\".name ");
        expected.Append("FROM ");
        expected.Append("users \"u\" ");
        expected.Append("WHERE ");
        expected.Append("\"u\".id > :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_Unaliased_CorrectSql()
    {
        DbTable u = new("users");

        SqlStatement sql =
            Select(u.Column("id"))
            .From(u)
            .Where(u.Column("id") > 0)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("id ");
        expected.Append("FROM ");
        expected.Append("users ");
        expected.Append("WHERE ");
        expected.Append("id > :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_InnerJoin_CorrectSql()
    {
        DbTable u = new("users", "u");
        DbTable o = new("orders", "o");

        SqlStatement sql =
            Select(u.Column("name"))
            .From(u)
            .InnerJoin(o)
            .On(u.Column("id") == o.Column("user_id"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"u\".name ");
        expected.Append("FROM ");
        expected.Append("users \"u\" ");
        expected.Append("INNER JOIN ");
        expected.Append("orders \"o\" ");
        expected.Append("ON ");
        expected.Append("\"u\".id = \"o\".user_id");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_Insert_CorrectSql()
    {
        DbTable u = new("users");

        SqlStatement sql =
            InsertInto(u, u.Column("id"), u.Column("name"))
            .Values(1, "Alice")
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("users ");
        expected.Append("(id, name) ");
        expected.Append("VALUES ");
        expected.Append("(:0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_Update_CorrectSql()
    {
        DbTable u = new("users");

        SqlStatement sql =
            Update(u)
            .Set(u.Column("name") == "Alice")
            .Where(u.Column("id") == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("users ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("id = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_Delete_CorrectSql()
    {
        DbTable u = new("users");

        SqlStatement sql =
            DeleteFrom(u)
            .Where(u.Column("id") == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("users ");
        expected.Append("WHERE ");
        expected.Append("id = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_Oracle_AliasedUpdate_CorrectSql()
    {
        DbTable u = new("users", "x");

        SqlStatement sql =
            Update(u)
            .Set(u.Column("name") == "Alice")
            .Where(u.Column("id") == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("users \"x\" ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"x\".id = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTableBase_TableName_ReturnsRawTableName()
    {
        Assert.Equal("test_table", new TestTable().TableName);
        Assert.Equal("test_table", new TestTable("t").TableName);
    }

    [Fact]
    public void DbTable_ColumnFromSourceColumn_CorrectSql()
    {
        DbTable u = new("users", "u");
        DbColumn source = new DbTable("orders", "o").Column("user_id");

        SqlStatement sql =
            Select(u.Column(source))
            .From(u)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"u\".user_id ");
        expected.Append("FROM ");
        expected.Append("users \"u\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DbTable_ColumnFromAlias_CorrectSql()
    {
        DbTable u = new("users", "u");
        ExpressionAlias userId = u.Column("id").As("uid");

        SqlStatement sql =
            Select(u.Column(userId))
            .From(u)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"u\".uid ");
        expected.Append("FROM ");
        expected.Append("users \"u\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
