using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ConditionIfTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public ConditionIfTests()
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
    public void ConditionIf_WhenConditionIsFalse_ThrowsArgumentException()
    {
        // An excluded condition used as the whole WHERE leaves nothing runnable,
        // so the clause is rejected at Build() rather than silently dropped (#236).
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Name).From(_t).Where(ConditionIf(false, _t.Code == 1)).Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void ConditionIf_MultiPartlyExcluded_CorrectSql() =>
        // Excluded operands drop out (ConditionIf's contract) while the one
        // included operand keeps the clause non-empty.
        _assert.Equal(
            ConditionIf(false, _t.Code == 1)
            & ConditionIf(false, _t.Code == 2)
            & ConditionIf(true, _t.Code == 3),
            "(\"t\".code = :0)", 1, 3);

    [Fact]
    public void ConditionIf_EmptyOrGroupBesideActive_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(
                (ConditionIf(false, _t.Code > 1) | ConditionIf(false, _t.Code > 2))
                & (_t.Code > 0))
            .Build();

        // The all-empty OR subtree drops out — no `()` — leaving the active operand.
        Assert.Equal("SELECT \"t\".code FROM test_table \"t\" WHERE (\"t\".code > :0)", sql.Text);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void ConditionIf_NotOverExcluded_ThrowsArgumentException()
    {
        // NOT over an empty operand is itself empty (never `NOT ()`) — rejected as a whole WHERE.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .Where(Not(ConditionIf(false, _t.Code > 0)))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }
}
