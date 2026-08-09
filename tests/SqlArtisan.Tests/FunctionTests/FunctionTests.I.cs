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
            Select(IntervalLiteral("30", Day()))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '30' DAY");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_WithFieldPrecision_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("300", Month(3)))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '300' MONTH(3)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_WithLeadingAndTrailingField_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("1-2", Year(), ToMonth))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '1-2' YEAR TO MONTH");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_LeadingFieldPrecision_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("123-11", Year(3), ToMonth))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '123-11' YEAR(3) TO MONTH");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_LeadingAndTrailingFieldPrecision_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("4 5:12:10.5", Day(3), ToSecond(2)))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '4 5:12:10.5' DAY(3) TO SECOND(2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_DayToHour_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("2 3", Day(), ToHour))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '2 3' DAY TO HOUR");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_DayToMinute_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("2 3:4", Day(), ToMinute))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '2 3:4' DAY TO MINUTE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_HourToMinute_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("3:4", Hour(), ToMinute))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '3:4' HOUR TO MINUTE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_HourToSecond_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("3:4:5", Hour(), ToSecond()))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '3:4:5' HOUR TO SECOND");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_Oracle_MinuteToSecond_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("4:5", Minute(), ToSecond()))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '4:5' MINUTE TO SECOND");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_SoleSecondField_CorrectSql()
    {
        SqlStatement sql =
            Select(IntervalLiteral("30", Second()))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INTERVAL '30' SECOND");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void IntervalLiteral_ReversedFieldRange_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => IntervalLiteral("30", Month(), Year()));

        Assert.Equal("INTERVAL MONTH TO YEAR is not a valid field range.", ex.Message);
    }

    [Fact]
    public void IntervalLiteral_SecondAsLeadingField_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => IntervalLiteral("30", Second(), ToMonth));

        Assert.Equal("INTERVAL SECOND TO MONTH is not a valid field range.", ex.Message);
    }

    [Fact]
    public void IntervalLiteral_CrossFamilyFieldRange_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => IntervalLiteral("30", Day(), ToMonth));

        Assert.Equal("INTERVAL DAY TO MONTH is not a valid field range.", ex.Message);
    }

    [Fact]
    public void Year_PrecisionAboveMaximum_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Year(10));

        Assert.Equal("YEAR precision must be between 0 and 9.", ex.Message);
    }

    [Fact]
    public void Year_PrecisionNegative_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Year(-1));

        Assert.Equal("YEAR precision must be between 0 and 9.", ex.Message);
    }

    [Fact]
    public void ToSecond_PrecisionAboveMaximum_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ToSecond(10));

        Assert.Equal("SECOND precision must be between 0 and 9.", ex.Message);
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
