using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Value_IntWithAlias_CorrectSql()
    {
        SqlStatement sql =
            Select(Value(1).As("depth"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT :0 \"depth\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Value_StringWithColumnAlias_CorrectSql()
    {
        var cte = new TestCte("org");

        SqlStatement sql =
            Select(Value("x").As(cte.CteCode))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT :0 cte_code");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Value_OperatorComposition_CorrectSql()
    {
        SqlStatement sql =
            Select(Value(1) + _t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT (:0 + \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Value_NullArgument_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Value(null!));
        Assert.Contains("Value cannot be null", ex.Message);
    }
}
