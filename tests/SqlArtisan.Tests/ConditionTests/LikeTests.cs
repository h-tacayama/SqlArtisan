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

    [Fact]
    public void Like_WithEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.Like("100%_off").Escape('!'),
            "\"t\".name LIKE :0 ESCAPE :1",
            2,
            "100%_off",
            '!');

    [Fact]
    public void NotLike_WithEscape_CorrectSql() =>
        _assert.Equal(
            _t.Name.NotLike("100%_off").Escape('!'),
            "\"t\".name NOT LIKE :0 ESCAPE :1",
            2,
            "100%_off",
            '!');
}
