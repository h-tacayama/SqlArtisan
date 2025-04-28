using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InsertTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void InsertInto_WithSetClause_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Set(
                _t.Code == 1,
                _t.Name == "a",
                _t.CreatedAt == SysDate)
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
            InsertInto(_t)
            .Set(
                _t.Code == Null,
                _t.Name == Null,
                _t.CreatedAt == Null)
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
    public void InsertInto_WithInequality_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            InsertInto(_t)
            .Set(
                _t.Code != Null)
            .Build();
        });
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
