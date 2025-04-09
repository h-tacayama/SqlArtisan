using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class OrderByTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void ORDER_BY_CharacterColumns_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.name)
            .FROM(_t)
            .ORDER_BY(
                _t.name,
                _t.name.ASC,
                _t.name.DESC,
                _t.name.NULLS_FIRST,
                _t.name.ASC.NULLS_FIRST,
                _t.name.DESC.NULLS_FIRST,
                _t.name.NULLS_LAST,
                _t.name.ASC.NULLS_LAST,
                _t.name.DESC.NULLS_LAST)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".name ASC, ");
        expected.Append("\"t\".name DESC, ");
        expected.Append("\"t\".name NULLS FIRST, ");
        expected.Append("\"t\".name ASC NULLS FIRST, ");
        expected.Append("\"t\".name DESC NULLS FIRST, ");
        expected.Append("\"t\".name NULLS LAST, ");
        expected.Append("\"t\".name ASC NULLS LAST, ");
        expected.Append("\"t\".name DESC NULLS LAST");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ORDER_BY_DateTimeColumns_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.created_at)
            .FROM(_t)
            .ORDER_BY(
                _t.created_at,
                _t.created_at.ASC,
                _t.created_at.DESC,
                _t.created_at.NULLS_FIRST,
                _t.created_at.ASC.NULLS_FIRST,
                _t.created_at.DESC.NULLS_FIRST,
                _t.created_at.NULLS_LAST,
                _t.created_at.ASC.NULLS_LAST,
                _t.created_at.DESC.NULLS_LAST)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".created_at, ");
        expected.Append("\"t\".created_at ASC, ");
        expected.Append("\"t\".created_at DESC, ");
        expected.Append("\"t\".created_at NULLS FIRST, ");
        expected.Append("\"t\".created_at ASC NULLS FIRST, ");
        expected.Append("\"t\".created_at DESC NULLS FIRST, ");
        expected.Append("\"t\".created_at NULLS LAST, ");
        expected.Append("\"t\".created_at ASC NULLS LAST, ");
        expected.Append("\"t\".created_at DESC NULLS LAST");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ORDER_BY_NumericColumns_CorrectSql()
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
    public void ORDER_BY_ColumnAliases_CorrectSql()
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
}
