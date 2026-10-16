using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// A column of an unaliased table renders bare, so a subquery that reads it
// without listing the table binds an inner same-named column instead (#677).
public class CorrelatedSubqueryTests
{
    private const string Message =
        "A table a correlated subquery references must be aliased.";

    [Fact]
    public void Exists_CorrelatedToUnaliasedTable_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(t.Name)
            .From(t)
            .Where(Exists(Select(a.Code).From(a).Where(a.Name == t.Name)))
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void In_CorrelatedToUnaliasedTable_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(t.Name)
            .From(t)
            .Where(t.Code.In(Select(a.Code).From(a).Where(a.Name == t.Name)))
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void ScalarSubquery_CorrelatedToUnaliasedTable_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(t.Name, Select(Max(a.Code)).From(a).Where(a.Name == t.Name).As("m"))
            .From(t)
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void CrossJoinLateral_CorrelatedToUnaliasedTable_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");
        DerivedTable x = new("x");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(t.Name)
            .From(t)
            .CrossJoinLateral(Select(a.Code).From(a).Where(a.Name == t.Name), x)
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void Exists_SelfCorrelationOverTwoUnaliasedInstances_ThrowsArgumentException()
    {
        TestTable outer = new();
        TestTable inner = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(outer.Name)
            .From(outer)
            .Where(Exists(Select(inner.Code).From(inner).Where(inner.Code == outer.Code)))
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void DoUpdateSet_SubqueryReadingUnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(t, t.Code, t.Name)
            .Values(1, "x")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name == Select(Max(a.Name)).From(a).Where(a.Code == t.Code))
            .Build());

        Assert.Equal(Message, ex.Message);
    }

    [Fact]
    public void Exists_CorrelatedToAliasedTable_CorrectSql()
    {
        TestTable t = new("t");
        ArchiveTable a = new();

        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .Where(Exists(Select(a.Code).From(a).Where(a.Name == t.Name)))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" "
                + "WHERE EXISTS (SELECT code FROM archive_table WHERE name = \"t\".name)",
            sql.Text);
    }

    [Fact]
    public void In_UncorrelatedSubqueryOverUnaliasedTables_CorrectSql()
    {
        TestTable t = new();
        ArchiveTable a = new();

        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .Where(t.Code.In(Select(a.Code).From(a).Where(a.Name == "x")))
            .Build();

        Assert.Equal(
            "SELECT name FROM test_table "
                + "WHERE code IN (SELECT code FROM archive_table WHERE name = :0)",
            sql.Text);
        Assert.Equal("x", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void In_SubqueryListingTheSameUnaliasedInstance_CorrectSql()
    {
        TestTable t = new();

        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .Where(t.Code == Select(Max(t.Code)).From(t))
            .Build();

        Assert.Equal(
            "SELECT name FROM test_table WHERE code = (SELECT MAX(code) FROM test_table)",
            sql.Text);
    }
}
