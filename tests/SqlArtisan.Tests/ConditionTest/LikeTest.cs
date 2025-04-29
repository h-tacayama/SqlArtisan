namespace InlineSqlSharp.Tests;

public class LikeTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public LikeTest()
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
}
