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
        expected.Append(") ");
        expected.Append("AND ");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(")");

        // When more than two conditions are connected with '&', 
        // the logic internally creates nested AndCondition objects.
        _assert.Equal(
            _t.Code == 1 & _t.Code == 2 & _t.Code == 3,
            expected.ToString(),
            3, 1, 2, 3);
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
        expected.Append(") ");
        expected.Append("OR ");
        expected.Append("(");
        expected.Append("\"t\".code = :2");
        expected.Append(")");

        // When more than two conditions are connected with '|', 
        // the logic internally creates nested OrCondition objects.
        _assert.Equal(
            _t.Code == 1 | _t.Code == 2 | _t.Code == 3,
            expected.ToString(),
            3, 1, 2, 3);
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
            (_t.Code == 1 | _t.Code == 2)
            & (_t.Code == 3 | _t.Code == 4),
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
            (_t.Code == 1 & _t.Code == 2)
            | (_t.Code == 3 & _t.Code == 4),
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
