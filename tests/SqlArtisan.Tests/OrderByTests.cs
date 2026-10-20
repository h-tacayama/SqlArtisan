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

        Assert.Equal(NullOrderByKeyMessage, ex.Message);
    }

    private const string NullOrderByKeyMessage =
        "An ORDER BY key cannot be a C# null; pass a column or an expression. "
            + "(Parameter 'orderByItems')";

    [Fact]
    public void OrderBy_WithNullItem_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            OrderBy(_t.Code, null!));

        Assert.Equal(NullOrderByKeyMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_StatementNullItems_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).OrderBy(null!));

        Assert.Equal(NullOrderByKeyMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_StatementNullItem_ThrowsArgumentNullException()
    {
        // Sql.Null is no remedy here either: PostgreSQL rejects it, SQLite sorts by a constant.
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).OrderBy(_t.Code, null!));

        Assert.Equal(NullOrderByKeyMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_ThrowsArgumentException()
    {
        // Dialect-blind and fixed at the call.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(0));

        Assert.Equal(
            "No engine accepts 0 as an ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_ZeroOrdinal_LeavesTheStageRetryable()
    {
        ISelectBuilderFrom held = Select(_t.Code).From(_t);
        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.OrderBy(0));

        SqlStatement sql = held.OrderBy(1).Build(Dbms.MySql);

        Assert.Equal(
            "No engine accepts 0 as an ORDER BY column ordinal; "
                + "order by a column, an expression, or a positive ordinal instead.",
            ex.Message);
        Assert.Equal("SELECT `t`.code FROM test_table `t` ORDER BY 1", sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
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
            "PostgreSQL, SQLite, and SQL Server do not accept a negative ORDER BY column "
                + "ordinal; order by a column, an expression, or a positive ordinal instead.",
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
    [InlineData(Dbms.SqlServer)]
    public void OrderBy_NegativeOrdinal_ThrowsOnTheEnginesThatReadItAsAPosition(Dbms dbms)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(-1).Build(dbms));

        Assert.Equal(
            "PostgreSQL, SQLite, and SQL Server do not accept a negative ORDER BY column "
                + "ordinal; order by a column, an expression, or a positive ordinal instead.",
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

    [Fact]
    public void OrderBy_Oracle_NegativeOrdinal_CorrectSql()
    {
        // Oracle reads it as a constant too (live-verified on XE 21.3.0, #523),
        // so the guard leaves that dialect alone.
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(-1).Build(Dbms.Oracle);

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" ORDER BY -1", sql.Text);
    }

    private const string NumberInExpressionMessage =
        "An ORDER BY inside OVER, WITHIN GROUP or an aggregate cannot sort by a number; "
            + "pass a column or an expression, or Bind(value) for a deliberate constant. "
            + "(Parameter 'orderByItems')";

    // #693: a number there is a constant key on PostgreSQL and SQLite, as #673
    // decided for PARTITION BY.
    public static TheoryData<object> NumbersInExpressionPosition => new()
    {
        1,
        0,
        -1,
        2L,
        2.5,
        2.5m,
        double.NaN,
    };

    [Theory]
    [MemberData(nameof(NumbersInExpressionPosition))]
    public void OrderBy_NumberInWindowPosition_ThrowsArgumentException(object key)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            RowNumber().Over(OrderBy(key)));

        Assert.Equal(NumberInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_NumberAfterPartitionBy_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            PartitionBy(_t.Name).OrderBy(_t.Code, 1));

        Assert.Equal(NumberInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_NumberInWithinGroupPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            StringAgg(_t.Code, ",").WithinGroup(OrderBy(0)));

        Assert.Equal(NumberInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_NumberInGroupConcatPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            GroupConcat(_t.Code, OrderBy(1)));

        Assert.Equal(NumberInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_ExplicitBindInWindowPosition_CorrectSql()
    {
        SqlStatement sql = Select(RowNumber().Over(OrderBy(Bind(1)))).From(_t).Build();

        Assert.Equal("SELECT ROW_NUMBER() OVER (ORDER BY :0) FROM test_table \"t\"", sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void OrderBy_SqlNullInWindowPosition_CorrectSql()
    {
        // The frame spelling with no ordering (public-api-design.md) stays legal.
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(Null).Rows(UnboundedPreceding)))
            .From(_t)
            .Build();

        Assert.Equal(
            "SELECT SUM(\"t\".code) OVER (ORDER BY NULL ROWS UNBOUNDED PRECEDING) "
                + "FROM test_table \"t\"",
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

    [Theory]
    [InlineData(Dbms.PostgreSql)]
    [InlineData(Dbms.SqlServer)]
    public void OrderBy_FractionalLiteral_ThrowsOnTheEnginesThatRejectIt(Dbms dbms)
    {
        // MySQL, SQLite and Oracle accept the no-op constant ordering;
        // PostgreSQL and SQL Server reject it (ADR 0011).
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(2.5).Build(dbms));

        Assert.Equal(
            "PostgreSQL and SQL Server do not accept a non-integer constant as an ORDER BY "
                + "sort key; order by a column or an expression instead.",
            ex.Message);
    }

    [Fact]
    public void OrderBy_Oracle_FractionalLiteral_CorrectSql()
    {
        // Oracle takes the no-op constant ordering (live-verified on XE 21.3.0,
        // #523), so the guard leaves that dialect alone.
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(2.5).Build(Dbms.Oracle);

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" ORDER BY 2.5", sql.Text);
    }

    [Fact]
    public void OrderBy_PostgreSql_IntegerOrdinal_CorrectSql()
    {
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(1).Build(Dbms.PostgreSql);

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" ORDER BY 1", sql.Text);
    }

    // #678: bound, each would be a constant sort key that sorts nothing.
    public static TheoryData<object, string> NonNumericSortKeys => new()
    {
        { "code", "System.String" },
        { 'c', "System.Char" },
        { true, "System.Boolean" },
        { new DateTime(2020, 1, 1), "System.DateTime" },
        { new DateOnly(2020, 1, 1), "System.DateOnly" },
        { new TimeOnly(1, 0), "System.TimeOnly" },
        { DayOfWeek.Monday, "System.DayOfWeek" },
        { DateTimePart.Year, "SqlArtisan.DateTimePart" },
    };

    [Theory]
    [MemberData(nameof(NonNumericSortKeys))]
    public void OrderBy_NonNumericValue_ThrowsArgumentException(object key, string typeName)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(_t.Code, key));

        Assert.Equal($"Invalid type for OrderByItem: {typeName}", ex.Message);
    }

    [Fact]
    public void OrderBy_NonNumericValueInWindowPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => OrderBy("code"));

        Assert.Equal("Invalid type for OrderByItem: System.String", ex.Message);
    }

    [Fact]
    public void OrderBy_ExplicitBind_CorrectSql()
    {
        SqlStatement sql = Select(_t.Code).From(_t).OrderBy(Bind("code")).Build();

        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" ORDER BY :0", sql.Text);
        Assert.Equal("code", sql.Parameters.Get<string>(":0"));
    }

    private const string AliasInExpressionMessage =
        "An ORDER BY inside OVER, WITHIN GROUP or an aggregate cannot sort by a select alias; "
            + "pass the aliased expression instead. (Parameter 'orderByItems')";

    [Fact]
    public void OrderBy_AliasInWindowPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => OrderBy(_t.Code.As("c")));

        Assert.Equal(AliasInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_SortedAliasInWindowPosition_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => OrderBy(_t.Code, (_t.Code + 1).As("c").Desc.NullsLast));

        Assert.Equal(AliasInExpressionMessage, ex.Message);
    }

    [Fact]
    public void PartitionByOrderBy_Alias_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => PartitionBy(_t.Name).OrderBy(_t.Code.As("c").Asc));

        Assert.Equal(AliasInExpressionMessage, ex.Message);
    }

    [Fact]
    public void OrderBy_AliasedExpressionPassedBare_InWindowPosition_CorrectSql()
    {
        SqlStatement sql = Select(RowNumber().Over(OrderBy((_t.Code + 1).Desc)))
            .From(_t)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT ROW_NUMBER() OVER (ORDER BY (\"t\".code + :0) DESC) FROM test_table \"t\"",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void OrderBy_NullOrderingRepeated_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => _t.Code.Desc.NullsFirst.NullsLast);

        Assert.Equal("A sort key takes one NULLS FIRST or NULLS LAST ordering.", ex.Message);
    }

    [Fact]
    public void OrderBy_SameNullOrderingRepeated_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => _t.Code.NullsLast.NullsLast);

        Assert.Equal("A sort key takes one NULLS FIRST or NULLS LAST ordering.", ex.Message);
    }
}
