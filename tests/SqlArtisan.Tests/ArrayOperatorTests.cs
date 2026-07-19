using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ArrayOperatorTests
{
    private readonly TestTable _t = new("t");

    // --- Array (ARRAY[...]) -----------------------------------------------------

    [Fact]
    public void Array_CorrectSql()
    {
        SqlStatement sql =
            Select(Array("a", "b"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ARRAY[:0, :1]", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Array_SingleElement_CorrectSql()
    {
        SqlStatement sql =
            Select(Array("a"))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ARRAY[:0]", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Array_ColumnElement_CorrectSql()
    {
        SqlStatement sql =
            Select(Array(_t.Name, "b"))
            .From(_t)
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT ARRAY[\"t\".name, :0] FROM test_table \"t\"", sql.Text);
        Assert.Equal("b", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Array_NoElements_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Array());

        Assert.Equal("ARRAY[...] requires at least one element.", ex.Message);
    }

    // --- ArrayOverlaps (&&) -----------------------------------------------------

    [Fact]
    public void ArrayOverlaps_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(ArrayOverlaps(_t.Name, Array("a", "b")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name && ARRAY[:0, :1]",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void ArrayOverlaps_WithAndCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(ArrayOverlaps(_t.Name, Array("a", "b")) & (_t.Name == "x"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE (\"t\".name && ARRAY[:0, :1]) AND (\"t\".name = :2)",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
        Assert.Equal("x", sql.Parameters.Get<string>(":2"));
    }

    // --- ArrayContains (@>) -----------------------------------------------------

    [Fact]
    public void ArrayContains_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(ArrayContains(_t.Name, Array("a")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name @> ARRAY[:0]",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
    }

    // --- ArrayContainedBy (<@) --------------------------------------------------

    [Fact]
    public void ArrayContainedBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(ArrayContainedBy(Array("a"), _t.Name))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE ARRAY[:0] <@ \"t\".name",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
    }
}
