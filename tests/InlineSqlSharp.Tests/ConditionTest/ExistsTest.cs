using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ExistsTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public ExistsTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void EXISTS_WithSimpleSelect_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("EXISTS ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(
            EXISTS(SELECT(2)),
            expected.ToString(),
            1, 2);
    }

    [Fact]
    public void EXISTS_WithSelectFrom_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("EXISTS ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");
        expected.Append(")");

        _assert.Equal(
            EXISTS(SELECT(_t.code).FROM(_t)),
            expected.ToString());
    }

    [Fact]
    public void EXISTS_WithSelectFromWhere_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("EXISTS ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".name = :1");
        expected.Append(")");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(")");

        _assert.Equal(
            AND(
                _t.code == 1,
                EXISTS(SELECT(_t.code).FROM(_t).WHERE(_t.name == "a")),
                _t.code == 2),
            expected.ToString(),
            3, 1, "a", 2);
    }

    [Fact]
    public void NOT_EXISTS_WithSelectFromWhere_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("NOT EXISTS ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");
        expected.Append(")");

        _assert.Equal(
            NOT_EXISTS(SELECT(_t.code).FROM(_t).WHERE(_t.code == 2)),
            expected.ToString(),
            1, 2);
    }
}
