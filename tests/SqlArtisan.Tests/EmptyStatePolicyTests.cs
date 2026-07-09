using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The #236 empty-state policy: a written condition clause with no runnable
// condition is rejected at Build() (never silently elided), uniformly across the
// positions below; an empty SELECT list throws eagerly. Omitting a clause is the
// way to express "no restriction".
public class EmptyStatePolicyTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly TestTable _unaliased = new();

    // Unaliased columns for the MERGE INSERT column list.
    private readonly TestTable _cols = new();

    [Fact]
    public void Where_AllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .Where(ConditionIf(false, _t.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void Where_EmptyOrGroupBesideActiveCondition_CorrectSql()
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
    public void Where_NotOverEmptyCondition_ThrowsArgumentException()
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

    [Fact]
    public void Having_AllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(_t.Code)
            .Having(ConditionIf(false, Count(_t.Code) > 1))
            .Build());

        Assert.Equal(
            "The HAVING clause requires a condition; omit it for no group restriction.",
            ex.Message);
    }

    [Fact]
    public void Filter_AllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Count(_t.Code).Filter(ConditionIf(false, _t.Code > 0)))
            .From(_t)
            .Build());

        Assert.Equal(
            "An aggregate's FILTER requires a condition; omit it for an unfiltered aggregate.",
            ex.Message);
    }

    [Fact]
    public void Filter_AllConditionsExcludedWithOver_ThrowsArgumentException()
    {
        // The windowed form reuses FilteredAggregateFunction — .Over(...) must not hide an empty FILTER.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Filter(ConditionIf(false, _t.Code > 0)).Over(PartitionBy(_t.Name)))
            .From(_t)
            .Build());

        Assert.Equal(
            "An aggregate's FILTER requires a condition; omit it for an unfiltered aggregate.",
            ex.Message);
    }

    [Fact]
    public void Update_WhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(_unaliased)
            .Set(_unaliased.Name == "x")
            .Where(ConditionIf(false, _unaliased.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void Update_NoWhere_CorrectSql()
    {
        // The legal twin: omitting WHERE is how an intentional full-table UPDATE
        // is spelled — it must still build.
        SqlStatement sql =
            Update(_unaliased)
            .Set(_unaliased.Name == "x")
            .Build();

        Assert.Equal("UPDATE test_table SET name = :0", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Update_EmptyConditionBesideActiveCondition_CorrectSql()
    {
        // A WHERE that is empty at the call but made non-empty by a later `&`
        // is legal — the guard runs at Build(), not eagerly.
        SqlStatement sql =
            Update(_unaliased)
            .Set(_unaliased.Name == "x")
            .Where(ConditionIf(false, _unaliased.Code > 0) & (_unaliased.Code == 1))
            .Build();

        Assert.Equal("UPDATE test_table SET name = :0 WHERE (code = :1)", sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
        Assert.Equal(1, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Delete_WhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_unaliased)
            .Where(ConditionIf(false, _unaliased.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void Join_OnAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .InnerJoin(_s)
            .On(ConditionIf(false, _t.Code == _s.Code))
            .Build());

        Assert.Equal(
            "A JOIN's ON clause requires a condition; use CrossJoin for an unconditional join.",
            ex.Message);
    }

    [Fact]
    public void LeftJoin_OnAllConditionsExcluded_ThrowsArgumentException()
    {
        // LEFT/RIGHT/FULL joins share the single OnClause guard with INNER JOIN.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .LeftJoin(_s)
            .On(ConditionIf(false, _t.Code == _s.Code))
            .Build());

        Assert.Equal(
            "A JOIN's ON clause requires a condition; use CrossJoin for an unconditional join.",
            ex.Message);
    }

    [Fact]
    public void Case_WhenAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Case(When(ConditionIf(false, _t.Code > 0)).Then(1)).As("x"))
            .From(_t)
            .Build());

        Assert.Equal("A CASE WHEN branch requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_OnAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(ConditionIf(false, _t.Code == _s.Code))
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE ON clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenMatchedConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched(ConditionIf(false, _s.Name == "x")).ThenUpdateSet(_t.Name == _s.Name)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE WHEN MATCHED AND clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenNotMatchedConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatched(ConditionIf(false, _s.Name == "x"))
                .ThenInsert(_cols.Code).Values(_s.Code)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE WHEN NOT MATCHED AND clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenNotMatchedBySourceConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatchedBySource(ConditionIf(false, _t.Name == "x")).ThenDelete()
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "A MERGE WHEN NOT MATCHED BY SOURCE AND clause requires a condition.",
            ex.Message);
    }

    [Fact]
    public void Merge_DeleteWhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .DeleteWhere(ConditionIf(false, _s.Name == "x"))
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE DELETE WHERE clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Select_NoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Select());

        Assert.Equal("SELECT requires at least one item.", ex.Message);
    }

    [Fact]
    public void Select_DistinctNoItems_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Select(Distinct));

        Assert.Equal("SELECT requires at least one item.", ex.Message);
    }
}
