using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ExistsTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public ExistsTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void Exists_WithSimpleSelect_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("EXISTS ");
        expected.Append("(");
        expected.Append("SELECT ");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(
            Exists(Select(2)),
            expected.ToString(),
            1, 2);
    }

    [Fact]
    public void Exists_WithSelectFrom_CorrectSql()
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
            Exists(Select(_t.Code).From(_t)),
            expected.ToString());
    }

    [Fact]
    public void Exists_WithSelectFromWhere_CorrectSql()
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
            _t.Code == 1
            & Exists(Select(_t.Code).From(_t).Where(_t.Name == "a"))
            & _t.Code == 2,
            expected.ToString(),
            3, 1, "a", 2);
    }

    [Fact]
    public void NotExists_WithSelectFromWhere_CorrectSql()
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
            NotExists(Select(_t.Code).From(_t).Where(_t.Code == 2)),
            expected.ToString(),
            1, 2);
    }
}
