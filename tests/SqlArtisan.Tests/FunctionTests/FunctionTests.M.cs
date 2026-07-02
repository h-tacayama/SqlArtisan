using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Match_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Match(_t.Name, _t.Code).Against("database"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT `t`.code ");
        expected.Append("FROM test_table `t` ");
        expected.Append("WHERE MATCH (`t`.name, `t`.code) AGAINST (?0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Match_MySql_InNaturalLanguageMode_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Match(_t.Name).Against("database", SearchModifier.InNaturalLanguageMode))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT `t`.code ");
        expected.Append("FROM test_table `t` ");
        expected.Append("WHERE MATCH (`t`.name) AGAINST (?0 IN NATURAL LANGUAGE MODE)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Match_MySql_InBooleanMode_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Match(_t.Name).Against("+data* -query", SearchModifier.InBooleanMode))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT `t`.code ");
        expected.Append("FROM test_table `t` ");
        expected.Append("WHERE MATCH (`t`.name) AGAINST (?0 IN BOOLEAN MODE)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("+data* -query", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Match_MySql_WithQueryExpansion_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Match(_t.Name).Against("database", SearchModifier.WithQueryExpansion))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT `t`.code ");
        expected.Append("FROM test_table `t` ");
        expected.Append("WHERE MATCH (`t`.name) AGAINST (?0 WITH QUERY EXPANSION)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Match_MySql_AgainstScore_CorrectSql()
    {
        SqlStatement sql =
            Select(Match(_t.Name).AgainstScore("database"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MATCH (`t`.name) AGAINST (?0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Match_Sqlite_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Match(_t, "database"))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE \"t\".test_table MATCH :0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Match_Sqlite_NoAlias_UsesTableName()
    {
        DbTable articles = new("articles");

        SqlStatement sql =
            Select(articles.Column("body"))
            .From(articles)
            .Where(Match(articles, "database"))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT body ");
        expected.Append("FROM articles ");
        expected.Append("WHERE articles MATCH :0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Max_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Max(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MAX(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Min_DateTimeValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Min(_t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MIN(\"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Mod_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Mod(_t.Code, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MOD(\"t\".code, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MonthsBetween_DateTimeValues_CorrectSql()
    {
        SqlStatement sql =
            Select(
                MonthsBetween(
                    ToDate("2001/02/03", "YYYY/MM/DD"),
                    ToDate("2004/05/06", "YYYY/MM/DD")))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("MONTHS_BETWEEN(TO_DATE(:0, :1), ");
        expected.Append("TO_DATE(:2, :3))");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
