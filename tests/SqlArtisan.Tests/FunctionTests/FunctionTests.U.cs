using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Unnest_SelectList_CorrectSql()
    {
        SqlStatement sql =
            Select(Unnest(Array("a", "b")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT UNNEST(ARRAY[:0, :1])", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Unnest_AsTable_CorrectSql()
    {
        int[] values = [1, 2, 3];
        UnnestDerivedTable t = Unnest(BindArray(values)).AsTable("t");

        SqlStatement sql =
            Select(t.Column("t"))
            .From(t)
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT \"t\".t FROM UNNEST(:0) \"t\"", sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Same(values, sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void Unnest_AsTable_NamedColumns_CorrectSql()
    {
        UnnestDerivedTable t =
            Unnest(BindArray([1, 2]), BindArray(["a", "b"]))
            .AsTable("t", "x", "y");

        SqlStatement sql =
            Select(t.Column("x"), t.Column("y"))
            .From(t)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".x, \"t\".y FROM UNNEST(:0, :1) \"t\" (x, y)",
            sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal([1, 2], sql.Parameters.Get<int[]>(":0"));
        Assert.Equal(["a", "b"], sql.Parameters.Get<string[]>(":1"));
    }

    [Fact]
    public void Unnest_JoinedWithTable_CorrectSql()
    {
        UnnestDerivedTable ids = Unnest(BindArray([1, 2])).AsTable("ids", "id");

        SqlStatement sql =
            Select(_t.Name)
            .From(_t, ids)
            .Where(_t.Code == ids.Column("id"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\", UNNEST(:0) \"ids\" (id)"
                + " WHERE \"t\".code = \"ids\".id",
            sql.Text);
        Assert.Equal([1, 2], sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void Unnest_NoArguments_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Unnest());

        Assert.Equal("UNNEST(...) requires at least one array.", ex.Message);
    }

    [Fact]
    public void Unnest_AsTable_NoColumns_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Unnest(BindArray([1])).AsTable("t", []));

        Assert.Equal(
            "An UNNEST column alias list requires at least one column.", ex.Message);
    }

    [Fact]
    public void Upper_CharacterValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Upper(_t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("UPPER(\"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
