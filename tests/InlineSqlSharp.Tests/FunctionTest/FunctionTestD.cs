using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void DECODE_WithCharacterExprDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        ("a", L("A")),
                        ('b', L("B")),
                    ],
                    L("unknown")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".name, :0, 'A', :1, 'B', 'unknown')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithStringDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        ("a", L("A")),
                        ('b', L("B")),
                    ],
                    "unknown"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".name, :0, 'A', :1, 'B', :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithCharDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        ("a", L("A")),
                        ('b', L("B")),
                    ],
                    'u'))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".name, :0, 'A', :1, 'B', :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithDateTimeExprDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.created_at,
                    [
                        (new DateTime(2001, 1, 1), (TO_DATE(L("2000/01/10"), L("YYYY/MM/DD")))),
                        (TO_DATE(L("2000/01/02"), L("YYYY/MM/DD")), new DateTime(2001, 1, 20)),
                    ],
                    _t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".created_at, ")
            .Append(":0, ")
            .Append("TO_DATE('2000/01/10', 'YYYY/MM/DD'), ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append(":1, ")
            .Append("\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithDateTimeDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.created_at,
                    [
                        (new DateTime(2001, 1, 1), (TO_DATE(L("2000/01/10"), L("YYYY/MM/DD")))),
                        (TO_DATE(L("2000/01/02"), L("YYYY/MM/DD")), new DateTime(2001, 1, 20)),
                    ],
                    new DateTime(2001, 1, 30)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".created_at, ")
            .Append(":0, ")
            .Append("TO_DATE('2000/01/10', 'YYYY/MM/DD'), ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append(":1, ")
            .Append(":2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithNumericExprDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (1, L(10)),
                        (2, L(20)),
                    ],
                    L(999)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, 10, :1, 20, 999)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithIntDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (1, L(10)),
                        (2, L(20)),
                    ],
                    999))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, 10, :1, 20, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_WithDoubleDefault_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (1, NULL),
                        (NULL, L(20)),
                    ],
                    999.9))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, NULL, NULL, 20, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
