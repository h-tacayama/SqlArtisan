using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class LikeTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public LikeTests()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void Like_String_CorrectSql() =>
        _assert.Equal(_t.Name.Like("%abc%"), "\"t\".name LIKE :0", 1, "%abc%");

    [Fact]
    public void NotLike_String_CorrectSql() =>
        _assert.Equal(_t.Name.NotLike("%abc%"), "\"t\".name NOT LIKE :0", 1, "%abc%");

    // The escape character is emitted as an inline string literal (not a bind
    // parameter), so only the pattern is parameterized.
    [Fact]
    public void Like_WithEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.Like("100%_off").Escape('!'),
            "\"t\".name LIKE :0 ESCAPE '!'",
            1,
            "100%_off");

    [Fact]
    public void NotLike_WithEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.NotLike("100%_off").Escape('!'),
            "\"t\".name NOT LIKE :0 ESCAPE '!'",
            1,
            "100%_off");

    // A single quote as the escape character is doubled inside the literal.
    [Fact]
    public void Like_WithSingleQuoteEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.Like("100'%").Escape('\''),
            "\"t\".name LIKE :0 ESCAPE ''''",
            1,
            "100'%");

    // PostgreSQL (standard-conforming strings): a backslash escape char stands for
    // itself and is not doubled.
    [Fact]
    public void Like_WithBackslashEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.Like("100\\%").Escape('\\'),
            "\"t\".name LIKE :0 ESCAPE '\\'",
            1,
            "100\\%");

    // MySQL treats the backslash as a string-literal escape, so it is doubled.
    [Fact]
    public void Like_MySql_WithBackslashEscape_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Name.Like("100\\%").Escape('\\'))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("WHERE ");
        expected.Append("`t`.name LIKE ?0 ESCAPE '\\\\'");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal("100\\%", sql.Parameters.Get<object>("?0"));
    }
}
