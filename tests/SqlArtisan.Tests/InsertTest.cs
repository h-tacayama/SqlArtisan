using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class InsertTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void InsertInto_WithoutColumnList_SqlWithValuesOnly()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Values(1, "a", SysDate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table \"t\" ");
        expected.Append("VALUES ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append("SYSDATE");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void InsertInto_WithColumnList_SqlWithColumnsAndValues()
    {
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
            .Values(1, "a", SysDate)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table \"t\" ");
        expected.Append("(");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".created_at");
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
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
            .Values(Null, Null, Null)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table \"t\" ");
        expected.Append("(");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".created_at");
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
        TestTable s = new("s");

        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
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
