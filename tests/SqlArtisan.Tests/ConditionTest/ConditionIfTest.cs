using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ConditionIfTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public ConditionIfTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void ConditionIf_WhenConditionIsTrue_CorrectSql() =>
        _assert.Equal(ConditionIf(true, _t.Code == 1),
            "\"t\".code = :0",
            1, 1);

    [Fact]
    public void ConditionIf_WhenConditionIsFalse_ReturnsEmpty() =>
        _assert.Equal(ConditionIf(false, _t.Code == 1), string.Empty);

    [Fact]
    public void ConditionIf_MultiEmptyCondition_ReturnsEmpty() =>
        _assert.Equal(
            ConditionIf(false, _t.Code == 1)
            & ConditionIf(false, _t.Code == 2)
            & ConditionIf(true, _t.Code == 3),
            "(\"t\".code = :0)", 1, 3);
}
