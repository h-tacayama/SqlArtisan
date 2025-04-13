using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CaseTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void CASE_SearchCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.name == L("a")).THEN(L("A")),
                        WHEN(_t.name == 'b').THEN('B'),
                        WHEN(_t.name == "c").THEN("C"),
                    ],
                    ELSE(L("z"))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".name = 'a') THEN 'A' ");
        expected.Append("WHEN (\"t\".name = :0) THEN :1 ");
        expected.Append("WHEN (\"t\".name = :2) THEN :3 ");
        expected.Append("ELSE 'z' ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(4, sql.Parameters.Count);
        Assert.Equal("b", sql.Parameters[0].Value);
        Assert.Equal("B", sql.Parameters[1].Value);
        Assert.Equal("c", sql.Parameters[2].Value);
        Assert.Equal("C", sql.Parameters[3].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithChar_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.name == L("a")).THEN(L("A")),
                    ],
                    ELSE('z')))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".name = 'a') THEN 'A' ");
        expected.Append("ELSE :0 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal("z", sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithString_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.name == L("a")).THEN(L("A")),
                    ],
                    ELSE("z")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".name = 'a') THEN 'A' ");
        expected.Append("ELSE :0 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal("z", sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithDateTimeExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.code == L(1)).THEN(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD"))),
                        WHEN(_t.code == L(2)).THEN(new DateTime(2004, 5, 6)),
                    ],
                    ELSE(_t.created_at)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".code = 1) THEN TO_DATE('2001/02/03', 'YYYY/MM/DD') ");
        expected.Append("WHEN (\"t\".code = 2) THEN :0 ");
        expected.Append("ELSE \"t\".created_at ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal(new DateTime(2004, 5, 6), sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithDateTime_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.code == L(1)).THEN(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD"))),
                    ],
                    ELSE(new DateTime(2007, 8, 9))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".code = 1) THEN TO_DATE('2001/02/03', 'YYYY/MM/DD') ");
        expected.Append("ELSE :0 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal(new DateTime(2007, 8, 9), sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithNumericExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.code == L(1)).THEN(L(2)),
                        WHEN(_t.code == L(3)).THEN(4),
                    ],
                    ELSE(L(99))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".code = 1) THEN 2 ");
        expected.Append("WHEN (\"t\".code = 3) THEN :0 ");
        expected.Append("ELSE 99 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal(4, sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SearchCaseWithInt_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.code == L(1)).THEN(L(2)),
                    ],
                    ELSE(99)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".code = 1) THEN 2 ");
        expected.Append("ELSE :0 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Single(sql.Parameters);
        Assert.Equal(99, sql.Parameters[0].Value);
    }

    [Fact]
    public void CASE_SimpleCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    _t.name,
                    [
                        WHEN(L("a")).THEN(L("A")),
                        WHEN('b').THEN('B'),
                        WHEN("c").THEN("C"),
                    ],
                    ELSE(L("z"))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("\"t\".name ");
        expected.Append("WHEN 'a' THEN 'A' ");
        expected.Append("WHEN :0 THEN :1 ");
        expected.Append("WHEN :2 THEN :3 ");
        expected.Append("ELSE 'z' ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(4, sql.Parameters.Count);
        Assert.Equal("b", sql.Parameters[0].Value);
        Assert.Equal("B", sql.Parameters[1].Value);
        Assert.Equal("c", sql.Parameters[2].Value);
        Assert.Equal("C", sql.Parameters[3].Value);
    }

    [Fact]
    public void CASE_SimpleCaseWithDateTimeExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    _t.created_at,
                    [
                        WHEN(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD"))).THEN(TO_DATE(L("2004/05/06"), L("YYYY/MM/DD"))),
                        WHEN(new DateTime(2007, 8, 9)).THEN(new DateTime(2010, 11, 12)),
                    ],
                    ELSE(_t.created_at)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("\"t\".created_at ");
        expected.Append("WHEN TO_DATE('2001/02/03', 'YYYY/MM/DD') THEN TO_DATE('2004/05/06', 'YYYY/MM/DD') ");
        expected.Append("WHEN :0 THEN :1 ");
        expected.Append("ELSE \"t\".created_at ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(new DateTime(2007, 8, 9), sql.Parameters[0].Value);
        Assert.Equal(new DateTime(2010, 11, 12), sql.Parameters[1].Value);
    }

    [Fact]
    public void CASE_SimpleCaseWithNumericExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    _t.code,
                    [
                        WHEN(L(1)).THEN(L(10)),
                        WHEN(2).THEN(20),
                    ],
                    ELSE(L(99))))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("\"t\".code ");
        expected.Append("WHEN 1 THEN 10 ");
        expected.Append("WHEN :0 THEN :1 ");
        expected.Append("ELSE 99 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(2, sql.Parameters[0].Value);
        Assert.Equal(20, sql.Parameters[1].Value);
    }
}
