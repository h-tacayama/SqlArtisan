using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class LogicalConditionTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public LogicalConditionTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void And_MultipleConditions_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :1");
        expected.Append(")");

        _assert.Equal(
            And(_t.Code == 1, _t.Code == 2),
            expected.ToString(),
            2, 1, 2);
    }

    [Fact]
    public void Or_MultipleConditions_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("OR ");
        expected.Append("(");
        expected.Append("\"t\".code = :1");
        expected.Append(")");

        _assert.Equal(
            Or(_t.Code == 1, _t.Code == 2),
            expected.ToString(),
            2, 1, 2);
    }

    [Fact]
    public void And_WithNestedOrConditions_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("OR ");
        expected.Append("(");
        expected.Append("\"t\".code = :1");
        expected.Append(")");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(") ");
        expected.Append("OR ");
        expected.Append("(");
        expected.Append("\"t\".code = :3");
        expected.Append(")");
        expected.Append(")");

        _assert.Equal(
            And(
                Or(_t.Code == 1, _t.Code == 2),
                Or(_t.Code == 3, _t.Code == 4)),
            expected.ToString(),
            4, 1, 2, 3, 4);
    }

    [Fact]
    public void Or_WithNestedAndConditions_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("(");
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :1");
        expected.Append(")");
        expected.Append(") ");
        expected.Append("OR ");
        expected.Append("(");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :3");
        expected.Append(")");
        expected.Append(")");

        _assert.Equal(
            Or(
                And(_t.Code == 1, _t.Code == 2),
                And(_t.Code == 3, _t.Code == 4)),
            expected.ToString(),
            4, 1, 2, 3, 4);
    }

    [Fact]
    public void Not_SingleCondition_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("NOT ");
        expected.Append("(");
        expected.Append("\"t\".code = :0");
        expected.Append(")");

        _assert.Equal(
            Not(_t.Code == 1),
            expected.ToString(),
            1, 1);
    }
}
