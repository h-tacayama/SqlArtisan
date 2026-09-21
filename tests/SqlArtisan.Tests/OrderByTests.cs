using System.Globalization;
using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class OrderByTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void OrderBy_WithColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(
                _t.Code,
                _t.Code.Asc,
                _t.Code.Desc,
                _t.Code.NullsFirst,
                _t.Code.Asc.NullsFirst,
                _t.Code.Desc.NullsFirst,
                _t.Code.NullsLast,
                _t.Code.Asc.NullsLast,
                _t.Code.Desc.NullsLast)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".code ASC, ");
        expected.Append("\"t\".code DESC, ");
        expected.Append("\"t\".code NULLS FIRST, ");
        expected.Append("\"t\".code ASC NULLS FIRST, ");
        expected.Append("\"t\".code DESC NULLS FIRST, ");
        expected.Append("\"t\".code NULLS LAST, ");
        expected.Append("\"t\".code ASC NULLS LAST, ");
        expected.Append("\"t\".code DESC NULLS LAST");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OrderBy_WithColumnAliases_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(
                _t.Name.As("a"),
                _t.CreatedAt.As("b").Asc,
                _t.Code.As("c").Desc,
                _t.Name.As("d").NullsFirst,
                _t.CreatedAt.As("e").NullsLast)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY ");
        expected.Append("\"a\", ");
        expected.Append("\"b\" ASC, ");
        expected.Append("\"c\" DESC, ");
        expected.Append("\"d\" NULLS FIRST, ");
        expected.Append("\"e\" NULLS LAST");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OrderBy_WithColumnNo_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code, _t.Name)
            .From(_t)
            .OrderBy(1, 2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY ");
        // SortOrder cannot be used when ORDER BY is specified with column numbers
        expected.Append("1, 2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void NullsFirst_HeldSortOrderDerivedAlongTwoBranches_BranchesStayIsolated()
    {
        // #399: NullsFirst/NullsLast mutated the receiving SortOrder in place,
        // so two derivations from one held order aliased to the last applied.
        SortOrder baseOrder = _t.Code.Asc;
        SortOrder branch1 = baseOrder.NullsFirst;
        SortOrder branch2 = baseOrder.NullsLast;

        SqlStatement baseSql = Select(_t.Code).From(_t).OrderBy(baseOrder).Build();
        SqlStatement branch1Sql = Select(_t.Code).From(_t).OrderBy(branch1).Build();
        SqlStatement branch2Sql = Select(_t.Code).From(_t).OrderBy(branch2).Build();

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" ORDER BY \"t\".code ASC",
            baseSql.Text);
        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" ORDER BY \"t\".code ASC NULLS FIRST",
            branch1Sql.Text);
        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" ORDER BY \"t\".code ASC NULLS LAST",
            branch2Sql.Text);
    }

    [Fact]
    public void OrderBy_NumericLiteralUnderCommaDecimalCulture_RendersInvariantSql()
    {
        // Arrange
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            string expected =
                "SELECT \"t\".code FROM test_table \"t\" ORDER BY 2.5";

            // Act
            SqlStatement sql = Select(_t.Code).From(_t).OrderBy(2.5).Build(Dbms.Sqlite);

            // Assert
            Assert.Equal(expected, sql.Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void OrderBy_WithNoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => OrderBy());

        Assert.Equal("ORDER BY requires at least one item.", ex.Message);
    }

    [Fact]
    public void OrderBy_WithNullItems_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderBy(null!));

        Assert.Equal("orderByItems", ex.ParamName);
    }

    [Fact]
    public void OrderBy_WithNullItem_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            OrderBy(_t.Code, null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'orderByItem')",
            ex.Message);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_ThrowsAtBuild()
    {
        // Dialect-blind, but statement-scoped: the throw is at Build(), because
        // a window's ordering reads the same literal as an expression.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(0).Build(Dbms.MySql));

        Assert.Equal(
            "No engine accepts 0 as an ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_InSubquery_ThrowsAtBuild()
    {
        // A nested block resolves its own ordinals, so the guard runs on every
        // query block, not only the one Build() was called on.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code.In(Select(_t.Code).From(_t).OrderBy(0)))
            .Build(Dbms.MySql));

        Assert.Equal(
            "No engine accepts 0 as an ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_NegativeOrdinal_InCteBody_ThrowsAtBuild()
    {
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(Select(_t.Code.As(cte.CteCode)).From(_t).OrderBy(-1)))
            .Select(cte.CteCode)
            .From(cte)
            .Build(Dbms.PostgreSql));

        Assert.Equal(
            "PostgreSQL and SQLite do not accept a negative ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_NanSortKey_ThrowsArgumentException()
    {
        // A value-domain failure gets a value message, not the type message.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(double.NaN));

        Assert.Equal("An ORDER BY numeric sort key must be finite.", ex.Message);
    }

    [Fact]
    public void OrderBy_InfiniteSortKey_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(float.PositiveInfinity));

        Assert.Equal("An ORDER BY numeric sort key must be finite.", ex.Message);
    }

    [Theory]
    [InlineData(Dbms.PostgreSql)]
    [InlineData(Dbms.Sqlite)]
    public void OrderBy_NegativeOrdinal_ThrowsOnTheEnginesThatReadItAsAPosition(Dbms dbms)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(-1).Build(dbms));

        Assert.Equal(
            "PostgreSQL and SQLite do not accept a negative ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_MySql_NegativeOrdinal_CorrectSql()
    {
        // MySQL reads a negative literal as a constant expression and orders by
        // nothing, so the ADR 0011 arm leaves it alone (live-verified on 8.0).
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(-1).Build(Dbms.MySql);

        Assert.Equal("SELECT `t`.code FROM test_table `t` ORDER BY -1", sql.Text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OrderBy_NonPositiveOrdinal_InWindowPosition_CorrectSql(int ordinal)
    {
        // OVER(...) reads the literal as an expression, and every engine here
        // takes it (live-verified on PostgreSQL 16 and SQLite 3.41/3.46).
        SqlStatement sql = Select(RowNumber().Over(OrderBy(ordinal)))
            .From(_t)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            $"SELECT ROW_NUMBER() OVER (ORDER BY {ordinal}) FROM test_table \"t\"",
            sql.Text);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_InWithinGroupPosition_CorrectSql()
    {
        SqlStatement sql = Select(StringAgg(_t.Code, ",").WithinGroup(OrderBy(0)))
            .From(_t)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT STRING_AGG(\"t\".code, ',') WITHIN GROUP (ORDER BY 0) "
                + "FROM test_table \"t\"",
            sql.Text);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_InGroupConcatPosition_CorrectSql()
    {
        SqlStatement sql = Select(GroupConcat(_t.Code, OrderBy(0)))
            .From(_t)
            .Build(Dbms.MySql);

        Assert.Equal(
            "SELECT GROUP_CONCAT(`t`.code ORDER BY 0) FROM test_table `t`",
            sql.Text);
    }

    [Fact]
    public void OrderBy_WholeDoubleLiteral_RendersDecimalPoint()
    {
        // A whole-valued double is a literal sort key, not a column ordinal, so
        // it keeps its decimal point ("2.0", never a bare "2").
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(2.0).Build(Dbms.Sqlite);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" ORDER BY 2.0",
            sql.Text);
    }

    [Fact]
    public void OrderBy_PostgreSql_FractionalLiteral_ThrowsArgumentException()
    {
        // MySQL and SQLite accept the no-op constant ordering; PostgreSQL
        // rejects it, and the analyzer cannot see a value (ADR 0011).
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(2.5).Build(Dbms.PostgreSql));

        Assert.Equal(
            "PostgreSQL does not accept a non-integer constant as an ORDER BY sort key; "
                + "order by a column or an expression instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_PostgreSql_IntegerOrdinal_CorrectSql()
    {
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(1).Build(Dbms.PostgreSql);

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" ORDER BY 1", sql.Text);
    }
}
