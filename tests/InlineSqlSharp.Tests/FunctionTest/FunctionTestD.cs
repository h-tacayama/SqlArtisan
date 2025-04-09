using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void DECODE_CharacterToCharacter_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        (L("a"), L("A")),
                        (L("b"), L("B")),
                    ],
                    L("unknown")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".name, 'a', 'A', 'b', 'B', 'unknown')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_DateTimeToCharacter_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.created_at,
                    [
                        (TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")), L("A")),
                        (TO_DATE(L("2000/01/02"), L("YYYY/MM/DD")), L("B")),
                    ],
                    L("unknown")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".created_at, ")
            .Append("TO_DATE('2000/01/01', 'YYYY/MM/DD'), ")
            .Append("'A', ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append("'B', ")
            .Append("'unknown')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_NumericToCharacter_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (L(1), L("A")),
                        (L(2), L("B")),
                    ],
                    L("unknown")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, 1, 'A', 2, 'B', 'unknown')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_CharacterToDateTime_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        (L("a"), TO_DATE(L("2000/01/01"), L("YYYY/MM/DD"))),
                        (L("b"), TO_DATE(L("2000/01/02"), L("YYYY/MM/DD"))),
                    ],
                    _t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".name, ")
            .Append("'a', ")
            .Append("TO_DATE('2000/01/01', 'YYYY/MM/DD'), ")
            .Append("'b', ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append("\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_DateTimeToDateTime_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.created_at,
                    [
                        (TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")), (TO_DATE(L("2000/01/10"), L("YYYY/MM/DD")))),
                        (TO_DATE(L("2000/01/02"), L("YYYY/MM/DD")), (TO_DATE(L("2000/01/20"), L("YYYY/MM/DD")))),
                    ],
                    _t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".created_at, ")
            .Append("TO_DATE('2000/01/01', 'YYYY/MM/DD'), ")
            .Append("TO_DATE('2000/01/10', 'YYYY/MM/DD'), ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append("TO_DATE('2000/01/20', 'YYYY/MM/DD'), ")
            .Append("\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_NumericToDateTime_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (L(1), TO_DATE(L("2000/01/01"), L("YYYY/MM/DD"))),
                        (L(2), TO_DATE(L("2000/01/02"), L("YYYY/MM/DD"))),
                    ],
                    _t.created_at))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".code, ")
            .Append("1, ")
            .Append("TO_DATE('2000/01/01', 'YYYY/MM/DD'), ")
            .Append("2, ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append("\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_CharacterToNumeric_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.name,
                    [
                        (L("a"), L(1)),
                        (L("b"), L(2)),
                    ],
                    L(999)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".name, 'a', 1, 'b', 2, 999)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_DateTimeToNumeric_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.created_at,
                    [
                        (TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")), L(1)),
                        (TO_DATE(L("2000/01/02"), L("YYYY/MM/DD")), L(2)),
                    ],
                    L(999)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ")
            .Append("DECODE(\"t\".created_at, ")
            .Append("TO_DATE('2000/01/01', 'YYYY/MM/DD'), ")
            .Append("1, ")
            .Append("TO_DATE('2000/01/02', 'YYYY/MM/DD'), ")
            .Append("2, ")
            .Append("999)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DECODE_NumericToNumeric_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    _t.code,
                    [
                        (L(1), L(10)),
                        (L(2), L(20)),
                    ],
                    L(999)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, 1, 10, 2, 20, 999)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
