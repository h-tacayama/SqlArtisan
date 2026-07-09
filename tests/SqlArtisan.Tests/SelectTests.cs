using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class SelectTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Select_WithoutTableAlias_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(
                t.Code,
                t.Name,
                t.CreatedAt)
            .From(t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("code, ");
        expected.Append("name, ");
        expected.Append("created_at ");
        expected.Append("FROM ");
        expected.Append("test_table");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_ColumnAliases_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code.As("code"),
                _t.Name.As("name"),
                _t.CreatedAt.As("登録日"))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code \"code\", ");
        expected.Append("\"t\".name \"name\", ");
        expected.Append("\"t\".created_at \"登録日\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_DistinctFromClause_CorrectSql()
    {
        SqlStatement sql =
            Select(Distinct, _t.Code)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_DistinctOn_CorrectSql()
    {
        SqlStatement sql =
            Select(DistinctOn(_t.Code), _t.Code, _t.Name)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT ON (\"t\".code) ");
        expected.Append("\"t\".code, \"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_DistinctOnMultipleExpressions_CorrectSql()
    {
        SqlStatement sql =
            Select(DistinctOn(_t.Code, _t.Name), _t.Code, _t.Name)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT ON (\"t\".code, \"t\".name) ");
        expected.Append("\"t\".code, \"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_DistinctOnWithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(DistinctOn(_t.Code), _t.Code, _t.Name)
            .From(_t)
            .OrderBy(_t.Code, _t.Name.Desc)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT ON (\"t\".code) ");
        expected.Append("\"t\".code, \"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY \"t\".code, \"t\".name DESC");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DistinctOn_NoExpressions_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Select(DistinctOn(), _t.Code).Build());
    }

    [Fact]
    public void Select_FromClauseWithMultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_FromDualClause_CorrectSql()
    {
        SqlStatement sql =
            Select(Sysdate)
            .From(Dual)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSDATE ");
        expected.Append("FROM ");
        expected.Append("DUAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_TableAliasWithDoubleQuotes_CorrectSql()
    {
        TestTable _t = new("t s");

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_WithHints_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Hints("/*+ ANY HINT */"),
                _t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("\"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_WithHintsAndDistinct_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Hints("/*+ ANY HINT */"),
                Distinct,
                _t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("DISTINCT ");
        expected.Append("\"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_WithHintsAndDistinctOn_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Hints("/*+ ANY HINT */"),
                DistinctOn(_t.Code),
                _t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("DISTINCT ON (\"t\".code) ");
        expected.Append("\"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_NoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Select());

        Assert.Equal("SELECT requires at least one item.", ex.Message);
    }

    [Fact]
    public void Select_DistinctNoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Select(Distinct));

        Assert.Equal("SELECT requires at least one item.", ex.Message);
    }
}
