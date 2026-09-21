using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class LogicalConditionTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public LogicalConditionTests()
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
    public void And_HeldConditionExtendedAlongTwoBranches_BranchesStayIsolated()
    {
        // #399: operator & used to mutate a held AndCondition in place, so
        // extending it along two branches leaked branch2's operand into branch1.
        SqlCondition baseCondition = (_t.Code == 1) & (_t.Code == 2);
        SqlCondition branch1 = baseCondition & (_t.Code == 3);
        SqlCondition branch2 = baseCondition & (_t.Code == 4);

        _assert.Equal(
            baseCondition,
            "(\"t\".code = :0) AND (\"t\".code = :1)",
            2, 1, 2);

        _assert.Equal(
            branch1,
            "(\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2)",
            3, 1, 2, 3);

        _assert.Equal(
            branch2,
            "(\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2)",
            3, 1, 2, 4);
    }

    [Fact]
    public void And_HeldThreeOperandConditionExtendedAlongTwoBranches_BranchesStayIsolated()
    {
        // Exercises the copy of an existing 3rd-and-later-operand array, not just
        // the binary base case.
        SqlCondition baseCondition = (_t.Code == 1) & (_t.Code == 2) & (_t.Code == 3);
        SqlCondition branch1 = baseCondition & (_t.Code == 4);
        SqlCondition branch2 = baseCondition & (_t.Code == 5);

        _assert.Equal(
            baseCondition,
            "(\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2)",
            3, 1, 2, 3);

        _assert.Equal(
            branch1,
            "(\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2) AND (\"t\".code = :3)",
            4, 1, 2, 3, 4);

        _assert.Equal(
            branch2,
            "(\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2) AND (\"t\".code = :3)",
            4, 1, 2, 3, 5);
    }

    [Fact]
    public void Or_HeldConditionExtendedAlongTwoBranches_BranchesStayIsolated()
    {
        SqlCondition baseCondition = (_t.Code == 1) | (_t.Code == 2);
        SqlCondition branch1 = baseCondition | (_t.Code == 3);
        SqlCondition branch2 = baseCondition | (_t.Code == 4);

        _assert.Equal(
            baseCondition,
            "(\"t\".code = :0) OR (\"t\".code = :1)",
            2, 1, 2);

        _assert.Equal(
            branch1,
            "(\"t\".code = :0) OR (\"t\".code = :1) OR (\"t\".code = :2)",
            3, 1, 2, 3);

        _assert.Equal(
            branch2,
            "(\"t\".code = :0) OR (\"t\".code = :1) OR (\"t\".code = :2)",
            3, 1, 2, 4);
    }

    [Fact]
    public void Or_HeldThreeOperandConditionExtendedAlongTwoBranches_BranchesStayIsolated()
    {
        SqlCondition baseCondition = (_t.Code == 1) | (_t.Code == 2) | (_t.Code == 3);
        SqlCondition branch1 = baseCondition | (_t.Code == 4);
        SqlCondition branch2 = baseCondition | (_t.Code == 5);

        _assert.Equal(
            baseCondition,
            "(\"t\".code = :0) OR (\"t\".code = :1) OR (\"t\".code = :2)",
            3, 1, 2, 3);

        _assert.Equal(
            branch1,
            "(\"t\".code = :0) OR (\"t\".code = :1) OR (\"t\".code = :2) OR (\"t\".code = :3)",
            4, 1, 2, 3, 4);

        _assert.Equal(
            branch2,
            "(\"t\".code = :0) OR (\"t\".code = :1) OR (\"t\".code = :2) OR (\"t\".code = :3)",
            4, 1, 2, 3, 5);
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
