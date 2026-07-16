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
    public void Asterisk_SelectList_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(Asterisk)
            .From(t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("* ");
        expected.Append("FROM ");
        expected.Append("test_table");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_AliasedTable_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Asterisk)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".* ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_MySql_AliasedTable_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Asterisk)
            .From(_t)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.* ");
        expected.Append("FROM ");
        expected.Append("test_table `t`");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_UnaliasedTable_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Asterisk)
            .From(t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("test_table.* ");
        expected.Append("FROM ");
        expected.Append("test_table");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_WithOtherSelectItem_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Asterisk, _t.Code)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".*, ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_Cte_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)))
            .Select(cte.Asterisk)
            .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\") ");
        expected.Append("SELECT \"cte\".* ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void QualifiedAsterisk_DerivedTable_CorrectSql()
    {
        TestDerivedTable x = new("x");

        SqlStatement sql =
            Select(x.Asterisk)
            .From(_t)
            .CrossApply(
                Select(_t.Code.As(x.Code), _t.Name.As(x.Total))
                    .From(_t),
                x)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"x\".* ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS APPLY ");
        expected.Append("(SELECT \"t\".code code, \"t\".name total FROM test_table \"t\") ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // The compiler blocks the marker in SqlExpression-typed positions; the
    // object-typed value positions reject it at runtime (ADR 0007 backstop).
    // COUNT is the one aggregate where * is legal — Count(Asterisk) has its
    // own overload (see FunctionTests.C).
    [Fact]
    public void Asterisk_InExpressionPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Sum(Asterisk));

        Assert.Equal(
            "Invalid type for SqlExpression: SqlArtisan.Internal.AsteriskMarker",
            ex.Message);
    }

    [Fact]
    public void Asterisk_InOrderBy_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(Asterisk).Build());

        Assert.Equal(
            "Invalid type for OrderByItem: SqlArtisan.Internal.AsteriskMarker",
            ex.Message);
    }

    [Fact]
    public void QualifiedAsterisk_InExpressionPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Upper(_t.Asterisk));

        Assert.Equal(
            "Invalid type for SqlExpression: SqlArtisan.Internal.QualifiedAsteriskMarker",
            ex.Message);
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Select_EmptyHints_CorrectSql(string? hints)
    {
        SqlStatement sql =
            Select(
                Hints(hints!),
                _t.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
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

    [Fact]
    public void Select_SqlServer_Top_CorrectSql()
    {
        SqlStatement sql = Select(Top(5), _t.Code).From(_t).Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TOP (@0) ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");
        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
    }

    [Fact]
    public void Select_SqlServer_TopWithTies_CorrectSql()
    {
        SqlStatement sql =
            Select(Top(5).WithTies(), _t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TOP (@0) WITH TIES ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY \"t\".code");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_SqlServer_DistinctTop_CorrectSql()
    {
        SqlStatement sql = Select(Distinct, Top(5), _t.Code).From(_t).Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT TOP (@0) ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_SqlServer_TopPercent_CorrectSql()
    {
        SqlStatement sql = Select(Top(10).Percent(), _t.Code).From(_t).Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("TOP (@0) PERCENT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Top_SqlServer_WithTiesNoOrderBy_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Top(5).WithTies(), _t.Code).From(_t).Build(Dbms.SqlServer));

        Assert.Equal("TOP ... WITH TIES requires an ORDER BY clause.", ex.Message);
    }

    [Fact]
    public void Top_SqlServer_WithOffset_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Top(5), _t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .OffsetRows(1)
            .FetchNext(2)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "TOP cannot be combined with OFFSET / FETCH on SQL Server; use one or the other.",
            ex.Message);
    }
}
