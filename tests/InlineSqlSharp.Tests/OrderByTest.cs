using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class OrderByTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void ORDER_BY_WithColumns_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .ORDER_BY(
                _t.code,
                _t.code.ASC,
                _t.code.DESC,
                _t.code.NULLS_FIRST,
                _t.code.ASC.NULLS_FIRST,
                _t.code.DESC.NULLS_FIRST,
                _t.code.NULLS_LAST,
                _t.code.ASC.NULLS_LAST,
                _t.code.DESC.NULLS_LAST)
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
    public void ORDER_BY_WithColumnAliases_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .ORDER_BY(
                _t.name.AS("a"),
                _t.created_at.AS("b").ASC,
                _t.code.AS("c").DESC,
                _t.name.AS("d").NULLS_FIRST,
                _t.created_at.AS("e").NULLS_LAST)
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
    public void ORDER_BY_WithColumnNo_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code, _t.name)
            .FROM(_t)
            .ORDER_BY(1, 2)
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
}
