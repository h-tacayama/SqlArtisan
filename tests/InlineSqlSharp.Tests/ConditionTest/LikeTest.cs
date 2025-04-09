using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LikeTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public LikeTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void LIKE_Literal_CorrectSql() =>
        _assert.Equal(_t.name.LIKE(L("%abc%")), "\"t\".name LIKE '%abc%'");

    [Fact]
    public void LIKE_String_CorrectSql() =>
        _assert.Equal(_t.name.LIKE("%abc%"), "\"t\".name LIKE :0", 1, "%abc%");

    [Fact]
    public void NOT_LIKE_Literal_CorrectSql() =>
        _assert.Equal(_t.name.NOT_LIKE(L("%abc%")), "\"t\".name NOT LIKE '%abc%'");

    [Fact]
    public void NOT_LIKE_String_CorrectSql() =>
        _assert.Equal(_t.name.NOT_LIKE("%abc%"), "\"t\".name NOT LIKE :0", 1, "%abc%");
}
