using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void PlaintoTsquery_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(PlaintoTsquery("web search"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("PLAINTO_TSQUERY(:0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("web search", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void PlaintoTsquery_WithConfig_CorrectSql()
    {
        SqlStatement sql =
            Select(PlaintoTsquery("english", "web search"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("PLAINTO_TSQUERY('english', :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("web search", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Power_BaseAndExponent_CorrectSql()
    {
        SqlStatement sql =
            Select(Power(_t.Code, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("POWER(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
