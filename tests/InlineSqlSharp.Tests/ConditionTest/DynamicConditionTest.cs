using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DynamicConditionTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public DynamicConditionTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void AddConditionIf_WhenConditionIsTrue_CorrectSql() =>
        _assert.Equal(AddConditionIf(true, _t.code == 1),
            "\"t\".code = :0",
            1, 1);

    [Fact]
    public void AddConditionIf_WhenConditionIsFalse_ReturnsEmpty() =>
        _assert.Equal(AddConditionIf(false, _t.code == 1), string.Empty);

    [Fact]
    public void AddConditionIf_MultiEmptyCondition_ReturnsEmpty() =>
        _assert.Equal(
            AND(
                AddConditionIf(false, _t.code == 1),
                AddConditionIf(false, _t.code == 2),
                AddConditionIf(true, _t.code == 3)),
            "(\"t\".code = :0)", 1, 3);
}
