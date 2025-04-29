using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class OrderByTest
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
}
