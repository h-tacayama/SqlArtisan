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
