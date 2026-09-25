using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class GroupByTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void GroupBy_SingleColumn_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupBy_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(
                _t.Code,
                _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_MySql_CorrectSql()
    {
        // MySQL has no GROUP BY ROLLUP(...) form (its spelling is the WITH ROLLUP
        // suffix, the separate .WithRollup() API); Rollup(...) emits faithfully.
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(`t`.code, `t`.name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void WithRollup_MySql_CorrectSql()
    {
        // MySQL's native grouping syntax: a WITH ROLLUP suffix on GROUP BY.
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(_t.Code, _t.Name)
            .WithRollup()
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code, `t`.name WITH ROLLUP");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void WithRollup_MySql_WithHaving_CorrectSql()
    {
        // The narrowed ISelectBuilderWithRollup still exposes Having; the suffix
        // renders right after the grouping list, before HAVING.
        SqlStatement sql =
            Select(
                _t.Code,
                Count(_t.Name))
            .From(_t)
            .GroupBy(_t.Code)
            .WithRollup()
            .Having(Count(_t.Name) > 1)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("COUNT(`t`.name) ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code WITH ROLLUP ");
        expected.Append("HAVING ");
        expected.Append("COUNT(`t`.name) > ?0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>("?0"));
    }

    [Fact]
    public void WithRollup_MySql_WithOrderBy_CorrectSql()
    {
        // WITH ROLLUP renders right after the grouping list, before later clauses.
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .GroupBy(_t.Code)
            .WithRollup()
            .OrderBy(_t.Code)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code WITH ROLLUP ");
        expected.Append("ORDER BY ");
        expected.Append("`t`.code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_Sqlite_CorrectSql()
    {
        // SQLite has no ROLLUP, but Build emits faithfully (ADR 0001/0003);
        // feasibility is the analyzer's concern, not a Build-time throw.
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .GroupBy(Rollup(_t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_MySql_CorrectSql()
    {
        // MySQL has no CUBE, but Build emits faithfully (ADR 0001/0003).
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE(`t`.code, `t`.name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_Sqlite_CorrectSql()
    {
        // SQLite has no CUBE, but Build emits faithfully (ADR 0001/0003).
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(
                Group(_t.Code),
                Group(_t.Name),
                Group()))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS(\"t\".code, \"t\".name, ())");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_CompositeSet_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(
                Group(_t.Code, _t.Name),
                Group(_t.CreatedAt),
                Group()))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS((\"t\".code, \"t\".name), \"t\".created_at, ())");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_CompositeGroup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(Group(_t.Code, _t.Name), _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP((\"t\".code, \"t\".name), \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_CompositeGroup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(Group(_t.Code, _t.Name), _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE((\"t\".code, \"t\".name), \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_MySql_CorrectSql()
    {
        // MySQL has no GROUPING SETS, but Build emits faithfully (ADR 0001/0003).
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(Group(_t.Code), Group(_t.Name)))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS(`t`.code, `t`.name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_Sqlite_CorrectSql()
    {
        // SQLite has no GROUPING SETS, but Build emits faithfully (ADR 0001/0003).
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(Group(_t.Code), Group(_t.Name)))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Grouping_MySql_WithRollup_CorrectSql()
    {
        // MySQL's WITH ROLLUP form combined with Grouping(...) to label subtotal rows.
        SqlStatement sql =
            Select(
                _t.Code,
                Grouping(_t.Code))
            .From(_t)
            .GroupBy(_t.Code)
            .WithRollup()
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("GROUPING(`t`.code) ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code WITH ROLLUP");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Grouping_CaseLabelsSubtotalRow_CorrectSql()
    {
        // GROUPING(...) distinguishes a ROLLUP subtotal row (1) from a genuine data
        // row (0), so it can drive a CASE label for the aggregated-away column —
        // a text column, since PostgreSQL rejects a CASE mixing integer and text.
        SqlStatement sql =
            Select(
                Case(
                    When(Grouping(_t.Name) == 1).Then("Total"),
                    Else(_t.Name)))
            .From(_t)
            .GroupBy(Rollup(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE WHEN (GROUPING(\"t\".name) = :0) THEN :1 ELSE \"t\".name END ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Total", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void GroupBy_WithNoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).GroupBy());

        Assert.Equal("GROUP BY requires at least one item.", ex.Message);
    }

    [Fact]
    public void GroupBy_WithNullItems_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).GroupBy(null!));

        Assert.Equal("groupByItems", ex.ParamName);
    }

    [Fact]
    public void Rollup_WithNullElements_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Rollup(_t.Code, null!));

        Assert.Equal("elements", ex.ParamName);
    }

    [Fact]
    public void Cube_WithNullElements_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Cube(_t.Code, null!));

        Assert.Equal("elements", ex.ParamName);
    }

    [Fact]
    public void GroupingSets_WithNullSets_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            GroupingSets(Group(_t.Code), null!));

        Assert.Equal(
            "GROUPING SETS must not contain a null grouping set. (Parameter 'sets')",
            ex.Message);
    }

    [Fact]
    public void GroupingSets_WithNullSet_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            GroupingSets(null!, Group(_t.Code)));

        Assert.Equal(
            "GROUPING SETS requires a grouping set. (Parameter 'set')",
            ex.Message);
    }

    // ── Ordinal group keys (#521 item 2) ─────────────────────────────

    [Fact]
    public void GroupBy_Ordinal_CorrectSql()
    {
        SqlStatement sql = Select(_t.Name).From(_t).GroupBy(1).Build();

        Assert.Equal("SELECT \"t\".name FROM test_table \"t\" GROUP BY 1", sql.Text);
    }

    // The headline example in the docs and CHANGELOG.
    [Fact]
    public void GroupBy_TwoOrdinals_CorrectSql()
    {
        SqlStatement sql = Select(_t.Name, _t.Code).From(_t).GroupBy(1, 2).Build();

        Assert.Equal(
            "SELECT \"t\".name, \"t\".code FROM test_table \"t\" GROUP BY 1, 2", sql.Text);
    }

    [Fact]
    public void GroupBy_OrdinalBesideColumn_CorrectSql()
    {
        SqlStatement sql = Select(_t.Name, _t.Code).From(_t).GroupBy(_t.Name, 2).Build();

        Assert.Equal(
            "SELECT \"t\".name, \"t\".code FROM test_table \"t\" GROUP BY \"t\".name, 2",
            sql.Text);
    }

    // Every engine refuses an ordinal below 1 — MySQL 8.0, Oracle XE 21.3.0,
    // PostgreSQL 16.13, SQLite 3.50.4 and SQL Server 2022 alike.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GroupBy_OrdinalBelowOne_ThrowsArgumentException(int ordinal)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Name).From(_t).GroupBy(ordinal));

        Assert.Equal("A GROUP BY column ordinal must be 1 or greater.", ex.Message);
    }

    // Rendered with a decimal point so a whole value cannot re-read as an
    // ordinal on the engines that take a fractional constant as one group.
    [Fact]
    public void GroupBy_WholeFractionalKey_KeepsItsDecimalPoint()
    {
        SqlStatement sql = Select(_t.Name).From(_t).GroupBy(2.0).Build();

        Assert.Equal("SELECT \"t\".name FROM test_table \"t\" GROUP BY 2.0", sql.Text);
    }

    [Fact]
    public void GroupBy_NonFiniteKey_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Name).From(_t).GroupBy(double.NaN));

        Assert.Equal("A GROUP BY numeric group key must be finite.", ex.Message);
    }
}
