using System.Text;

namespace SqlArtisan.Tests;

public class InTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public InTests()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void In_SingleInt_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0");
        expected.Append(")");

        _assert.Equal(_t.Code.In(2), expected.ToString(), 1, 2);
    }

    [Fact]
    public void In_MultipleInts_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.Code.In(1, 2, 3),
            expected.ToString(),
            3, 1, 2, 3);
    }

    [Fact]
    public void NotIn_MultipleEnums_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".code NOT IN ");
        expected.Append("(");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2");
        expected.Append(")");

        _assert.Equal(_t.Code.NotIn(TestEnum.One, TestEnum.Two, TestEnum.Three),
            expected.ToString(),
            3, TestEnum.One, TestEnum.Two, TestEnum.Three);
    }

    [Fact]
    public void In_ListCollection_CorrectSql()
    {
        // A List binds to the IReadOnlyCollection<T> overload — no .Cast<object>().ToArray().
        List<int> codes = [1, 2, 3];

        StringBuilder expected = new();
        expected.Append("\"t\".code IN ");
        expected.Append("(:0, :1, :2)");

        _assert.Equal(_t.Code.In(codes), expected.ToString(), 3, 1, 2, 3);
    }

    [Fact]
    public void NotIn_ArrayCollection_CorrectSql()
    {
        int[] codes = [4, 5];

        StringBuilder expected = new();
        expected.Append("\"t\".code NOT IN ");
        expected.Append("(:0, :1)");

        _assert.Equal(_t.Code.NotIn(codes), expected.ToString(), 2, 4, 5);
    }

    [Fact]
    public void In_StringArrayCollection_CorrectSql()
    {
        // A string[] is a collection of strings — one bind per string, not per char.
        string[] names = ["a", "b"];

        StringBuilder expected = new();
        expected.Append("\"t\".name IN ");
        expected.Append("(:0, :1)");

        _assert.Equal(_t.Name.In(names), expected.ToString(), 2, "a", "b");
    }

    [Fact]
    public void In_SingleString_BindsOneValueNotOnePerChar()
    {
        // Regression guard: a lone string must not expand into char binds. Because
        // string is not IReadOnlyCollection<char>, it routes to the params overload.
        StringBuilder expected = new();
        expected.Append("\"t\".name IN ");
        expected.Append("(:0)");

        _assert.Equal(_t.Name.In("abc"), expected.ToString(), 1, "abc");
    }

    [Fact]
    public void NotIn_SingleString_BindsOneValueNotOnePerChar()
    {
        StringBuilder expected = new();
        expected.Append("\"t\".name NOT IN ");
        expected.Append("(:0)");

        _assert.Equal(_t.Name.NotIn("abc"), expected.ToString(), 1, "abc");
    }

    [Fact]
    public void In_EmptyCollection_ThrowsArgumentException()
    {
        List<int> empty = [];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _t.Code.In(empty));
        Assert.Equal("IN requires at least one value.", ex.Message);
    }

    [Fact]
    public void NotIn_EmptyCollection_ThrowsArgumentException()
    {
        int[] empty = [];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _t.Code.NotIn(empty));
        Assert.Equal("NOT IN requires at least one value.", ex.Message);
    }
}
