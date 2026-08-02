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
        // #399: NullsFirst/NullsLast used to mutate the receiving SortOrder in
        // place, so two derivations from one held SortOrder aliased to whichever
        // was applied last.
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
    public void OrderBy_WithNoItems_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrderBy());
    }

    [Fact]
    public void OrderBy_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OrderBy(null!));
    }
}
