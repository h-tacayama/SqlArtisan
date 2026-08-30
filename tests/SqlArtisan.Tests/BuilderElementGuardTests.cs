using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The element-guard class: a computed null element in a typed params array owes
// a named ArgumentNullException (guards-and-empty-states rule, element clause).
// FactoryGuardSweepTests mechanizes the Sql.* factory side; these pin the
// builder instance-method and clause-constructor sites it cannot reach.
public class BuilderElementGuardTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void From_NullTableElement_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t, null!));

        Assert.Equal(
            "A FROM clause must not contain a null table reference. (Parameter 'tables')",
            ex.Message);
    }

    [Fact]
    public void GroupingSets_NullSetElement_ThrowsArgumentNullException()
    {
        // Three arguments so the trailing null is an element of the params
        // array, not the array itself (which C# binds for a two-argument call).
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            GroupingSets(Group(_t.Code), Group(_t.Name), null!));

        Assert.Equal(
            "GROUPING SETS must not contain a null grouping set. (Parameter 'sets')",
            ex.Message);
    }

    [Fact]
    public void JoinUsing_NullColumnElement_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).InnerJoin(_s).Using(_t.Code, _t.Name, null!));

        Assert.Equal(
            "A USING column list must not contain a null column. (Parameter 'additionalColumns')",
            ex.Message);
    }

    [Fact]
    public void JoinUsing_NullParamsArray_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).InnerJoin(_s).Using(_t.Code, null!));

        Assert.Equal(
            "A USING column list must not contain a null column. (Parameter 'additionalColumns')",
            ex.Message);
    }

    [Fact]
    public void OnConflict_NullColumnElement_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertInto(t, t.Code).Values(1).OnConflict(t.Code, null!));

        Assert.Equal(
            "An ON CONFLICT target must not contain a null column. (Parameter 'conflictTarget')",
            ex.Message);
    }

    [Fact]
    public void OutputInto_NullColumnElement_ThrowsArgumentNullException()
    {
        TestTable t = new();
        TestTable archive = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            DeleteFrom(t)
                .Output(Deleted(t.Code))
                .Into(archive, archive.Code, null!));

        Assert.Equal(
            "An OUTPUT INTO column list must not contain a null column. (Parameter 'columns')",
            ex.Message);
    }

    [Fact]
    public void ThenInsert_EmptyColumns_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
                .Using(_s)
                .On(_t.Code == _s.Code)
                .WhenNotMatched()
                .ThenInsert([]));

        Assert.Equal("An INSERT column list requires at least one column.", ex.Message);
    }

    [Fact]
    public void ThenInsert_NullColumnElement_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MergeInto(_t)
                .Using(_s)
                .On(_t.Code == _s.Code)
                .WhenNotMatched()
                .ThenInsert(_t.Code, null!));

        Assert.Equal(
            "An INSERT column list must not contain a null column. (Parameter 'columns')",
            ex.Message);
    }

    [Fact]
    public void Using_NullTableElement_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            DeleteFrom(_t).Using(_s, null!));

        Assert.Equal(
            "A USING clause must not contain a null table reference. (Parameter 'tables')",
            ex.Message);
    }

    [Fact]
    public void Values_NullRow_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertInto(t, t.Code, t.Name).Values([null!, [2, "b"]]));

        Assert.Equal(
            "A VALUES source must not contain a null row. (Parameter 'rows')",
            ex.Message);
    }

    [Fact]
    public void Values_NullSecondRow_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            InsertInto(t, t.Code, t.Name).Values([[1, "a"], null!]));

        Assert.Equal(
            "A VALUES source must not contain a null row. (Parameter 'rows')",
            ex.Message);
    }

    [Fact]
    public void With_NullCteElement_ThrowsArgumentNullException()
    {
        Cte c = new("c");
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            With(c.As(Select(t.Code.As(c.Column("code"))).From(t)), null!));

        Assert.Equal(
            "A WITH clause must not contain a null CTE definition. (Parameter 'ctes')",
            ex.Message);
    }

    [Fact]
    public void WithBuilderInsertIgnoreInto_EmptyColumns_ThrowsArgumentException()
    {
        Cte c = new("c");
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(c.As(Select(t.Code.As(c.Column("code"))).From(t)))
                .InsertIgnoreInto(new TestTable(), []));

        Assert.Equal("An INSERT column list requires at least one column.", ex.Message);
    }

    [Fact]
    public void WithBuilderInsertInto_EmptyColumns_ThrowsArgumentException()
    {
        Cte c = new("c");
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(c.As(Select(t.Code.As(c.Column("code"))).From(t)))
                .InsertInto(new TestTable(), []));

        Assert.Equal("An INSERT column list requires at least one column.", ex.Message);
    }

    [Fact]
    public void WithBuilderInsertInto_NullColumnElement_ThrowsArgumentNullException()
    {
        Cte c = new("c");
        TestTable t = new();
        TestTable target = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            With(c.As(Select(t.Code.As(c.Column("code"))).From(t)))
                .InsertInto(target, target.Code, null!));

        Assert.Equal(
            "An INSERT column list must not contain a null column. (Parameter 'columns')",
            ex.Message);
    }

    [Fact]
    public void WithRecursive_NullCteElement_ThrowsArgumentNullException()
    {
        Cte c = new("c");
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            WithRecursive(c.As(Select(t.Code.As(c.Column("code"))).From(t)), null!));

        Assert.Equal(
            "A WITH clause must not contain a null CTE definition. (Parameter 'ctes')",
            ex.Message);
    }
}
