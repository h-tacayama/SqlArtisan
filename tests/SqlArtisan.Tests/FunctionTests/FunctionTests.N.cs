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
