using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class SqlStatementTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void ToString_WithParameters_ReturnsText()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build();

        Assert.Equal(sql.Text, sql.ToString());
        Assert.Equal("SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = :0", sql.ToString());
    }

    [Fact]
    public void ToString_NoParameters_ReturnsText()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Build();

        Assert.Equal(sql.Text, sql.ToString());
        Assert.Equal("SELECT \"t\".name FROM test_table \"t\"", sql.ToString());
    }
}
