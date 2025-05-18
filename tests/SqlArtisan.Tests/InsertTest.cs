using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class InsertTest
{
    [Fact]
    public void InsertInto_WithValuesClause_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Values(1, "a", SysDate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append("(");
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append("SYSDATE");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithNull_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Values(Null, Null, Null)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table ");
        expected.Append("(");
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at");
        expected.Append(") ");
        expected.Append("VALUES ");
        expected.Append("(");
        expected.Append("NULL, ");
        expected.Append("NULL, ");
        expected.Append("NULL");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithSelectClause_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name, t.CreatedAt)
            .Select(s.Code, s.Name, s.CreatedAt)
            .From(s)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table \"t\" ");
        expected.Append("(");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".created_at");
        expected.Append(") ");
        expected.Append("SELECT ");
        expected.Append("\"s\".code, ");
        expected.Append("\"s\".name, ");
        expected.Append("\"s\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
