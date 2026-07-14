using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class NoConditionTests
{
    private readonly TestTable _t;

    public NoConditionTests()
    {
        _t = new TestTable("t");
    }

    [Fact]
    public void NoCondition_AndFoldOfManyConditions_CorrectSql()
    {
        SqlCondition[] filters =
        [
            _t.Code == 1,
            _t.Code == 2,
            _t.Code == 3,
        ];

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(filters.Aggregate(NoCondition, (a, b) => a & b))
            .Build();

        // The seed drops out; the three real conditions fold into one AND group.
        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" "
            + "WHERE (\"t\".code = :0) AND (\"t\".code = :1) AND (\"t\".code = :2)",
            sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
    }

    [Fact]
    public void NoCondition_AndFoldOfOneCondition_CorrectSql()
    {
        SqlCondition[] filters = [_t.Code == 1];

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(filters.Aggregate(NoCondition, (a, b) => a & b))
            .Build();

        // A single real condition beside the seed leaves just that condition.
        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".code = :0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
    }

    [Fact]
    public void NoCondition_OrFold_CorrectSql()
    {
        SqlCondition[] filters =
        [
            _t.Code == 1,
            _t.Code == 2,
        ];

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(filters.Aggregate(NoCondition, (a, b) => a | b))
            .Build();

        // Neutral in an OR fold too — no absorbing TRUE, so both operands survive.
        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" "
            + "WHERE (\"t\".code = :0) OR (\"t\".code = :1)",
            sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
    }

    [Fact]
    public void NoCondition_EmptyFoldAsWholeWhere_ThrowsArgumentException()
    {
        SqlCondition[] filters = [];

        // A fold of zero conditions reduces to the seed alone — an empty WHERE,
        // rejected at Build() (#236) rather than run unfiltered by accident.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Name)
            .From(_t)
            .Where(filters.Aggregate(NoCondition, (a, b) => a & b))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void NoCondition_ReusedAcrossStatements_IsShareSafe()
    {
        // The property hands out a shared singleton; folding onto it must not
        // corrupt a later fold that starts from the same seed.
        SqlStatement first =
            Select(_t.Name)
            .From(_t)
            .Where(NoCondition & (_t.Code == 1))
            .Build();

        SqlStatement second =
            Select(_t.Name)
            .From(_t)
            .Where(NoCondition & (_t.Code == 2))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".code = :0)",
            first.Text);
        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".code = :0)",
            second.Text);
        Assert.Equal(1, first.Parameters.Get<int>(":0"));
        Assert.Equal(2, second.Parameters.Get<int>(":0"));
    }
}
