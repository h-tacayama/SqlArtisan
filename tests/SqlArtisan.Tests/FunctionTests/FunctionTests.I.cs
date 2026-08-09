using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Instr_BasicPattern_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc"))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Instr_WithPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc", 1))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Instr_WithOccurrence_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc", 1, 2))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Interval_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(Interval(30, DateTimePart.Day))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL ?0 DAY");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(30, sql.Parameters.Get<int>("?0"));
    }

    [Fact]
    public void Interval_MySql_WithArithmetic_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.CreatedAt - Interval(30, DateTimePart.Day))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(`t`.created_at - INTERVAL ?0 DAY)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Interval_NullQuantity_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => Interval(null!, DateTimePart.Day));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'item')",
            ex.Message);
    }

    [Fact]
    public void IntervalLiteral_PostgreSql_TextOnly_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("30 days"))
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '30 days'");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void IntervalLiteral_PostgreSql_WithArithmetic_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.CreatedAt - IntervalLiteral("30 days"))
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(\"t\".created_at - INTERVAL '30 days')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_WithField_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("30", DateTimePart.Day))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '30' DAY");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_WithLeadingAndTrailingField_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("1-2", DateTimePart.Year, DateTimePart.Month))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '1-2' YEAR TO MONTH");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_ValueWithQuote_EscapesLiteral()
    {
        SqlStatement sql =
            Select(IntervalLiteral("30'; DROP TABLE t --"))
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '30''; DROP TABLE t --'");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_NullValue_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => IntervalLiteral(null!));

        Assert.Equal("INTERVAL requires a literal value.", ex.Message);
    }

    [Fact]
    public void IntervalLiteral_EmptyValue_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => IntervalLiteral(""));

        Assert.Equal("INTERVAL requires a literal value.", ex.Message);
    }
}
