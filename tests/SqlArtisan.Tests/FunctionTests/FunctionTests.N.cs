using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Nextval_SequenceName_CorrectSql()
    {
        SqlStatement sql =
            Select(Nextval("seq_test"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXTVAL('seq_test')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Nextval_SequenceNameWithQuote_EscapesLiteral()
    {
        SqlStatement sql =
            Select(Nextval("seq'; DROP TABLE users; --"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXTVAL('seq''; DROP TABLE users; --')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Nextval_MySql_SequenceNameWithBackslash_EscapesLiteral()
    {
        SqlStatement sql =
            Select(Nextval("se'q\\x"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXTVAL('se''q\\\\x')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Nextval_NullSequenceName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Nextval(null!));

        Assert.Equal("NEXTVAL requires a sequence name.", ex.Message);
    }

    [Fact]
    public void Nextval_EmptySequenceName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Nextval(""));

        Assert.Equal("NEXTVAL requires a sequence name.", ex.Message);
    }

    [Fact]
    public void NextValueFor_SequenceName_CorrectSql()
    {
        SqlStatement sql =
            Select(NextValueFor("seq_test"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXT VALUE FOR seq_test");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // The sequence name reaches an identifier position here, unlike the
    // PostgreSQL spelling above, so it is emitted verbatim (ADR 0016).
    [Fact]
    public void NextValueFor_SequenceNameWithQuote_EmitsVerbatim()
    {
        SqlStatement sql =
            Select(NextValueFor("seq\"; DROP TABLE users; --"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NEXT VALUE FOR seq\"; DROP TABLE users; --");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void NextValueFor_NullSequenceName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => NextValueFor(null!));

        Assert.Equal("NEXT VALUE FOR requires a sequence name.", ex.Message);
    }

    [Fact]
    public void NextValueFor_EmptySequenceName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => NextValueFor(""));

        Assert.Equal("NEXT VALUE FOR requires a sequence name.", ex.Message);
    }

    [Fact]
    public void NextValueFor_WhiteSpaceSequenceName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => NextValueFor(" "));

        Assert.Equal("NEXT VALUE FOR requires a sequence name.", ex.Message);
    }

    [Fact]
    public void Nullif_ColumnAndValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Nullif(_t.Code, 0))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NULLIF(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Numtodsinterval_BoundQuantity_CorrectSql()
    {
        SqlStatement sql =
            Select(Numtodsinterval(30, DateTimePart.Day))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NUMTODSINTERVAL(:0, 'DAY')");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(30, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Numtodsinterval_ColumnQuantity_CorrectSql()
    {
        SqlStatement sql =
            Select(Numtodsinterval(_t.Code, DateTimePart.Hour))
            .From(_t)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NUMTODSINTERVAL(\"t\".code, 'HOUR') ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Theory]
    [InlineData(DateTimePart.Year)]
    [InlineData(DateTimePart.Month)]
    [InlineData(DateTimePart.Week)]
    public void Numtodsinterval_InvalidUnit_ThrowsArgumentException(DateTimePart unit)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Numtodsinterval(1, unit));

        Assert.Equal("NUMTODSINTERVAL requires an interval unit of DAY, HOUR, MINUTE, or SECOND.", ex.Message);
    }

    [Fact]
    public void Numtoyminterval_BoundQuantity_CorrectSql()
    {
        SqlStatement sql =
            Select(Numtoyminterval(3, DateTimePart.Month))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NUMTOYMINTERVAL(:0, 'MONTH')");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Numtoyminterval_InvalidUnit_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Numtoyminterval(1, DateTimePart.Day));

        Assert.Equal("NUMTOYMINTERVAL requires an interval unit of YEAR or MONTH.", ex.Message);
    }

    [Fact]
    public void Nvl_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Nvl(_t.Name, "Unknown"))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("NVL(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
