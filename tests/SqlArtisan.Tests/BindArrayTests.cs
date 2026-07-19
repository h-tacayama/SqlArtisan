using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class BindArrayTests
{
    private readonly TestTable _t = new("t");

    // --- BindArray + ANY --------------------------------------------------------

    [Fact]
    public void BindArray_EqualsAny_CorrectSql()
    {
        int[] values = [1, 2, 3];

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == Any(BindArray(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Same(values, sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void BindArray_Collection_BindsSingleParameter()
    {
        List<string> values = ["a", "b"];

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Name == Any(BindArray(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".name = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(["a", "b"], sql.Parameters.Get<string[]>(":0"));
    }

    [Fact]
    public void BindArray_EmptyArray_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == Any(BindArray(System.Array.Empty<int>())))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Empty(sql.Parameters.Get<int[]>(":0")!);
    }

    [Fact]
    public void BindArray_NullableElements_CorrectSql()
    {
        int?[] values = [1, null];

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == Any(BindArray(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Same(values, sql.Parameters.Get<int?[]>(":0"));
    }

    [Fact]
    public void BindArray_SharedInstance_ReusesMarker()
    {
        BindArrayValue shared = BindArray([1, 2]);

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where((_t.Code == Any(shared)) & (_t.Code != All(shared)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\""
                + " WHERE (\"t\".code = ANY (:0)) AND (\"t\".code <> ALL (:0))",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
    }

    // --- ALL / SOME -------------------------------------------------------------

    [Fact]
    public void QuantifiedExpression_All_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code < All(BindArray([10, 20])))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code < ALL (:0)",
            sql.Text);
        Assert.Equal([10, 20], sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void QuantifiedExpression_Some_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == Some(BindArray([1])))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = SOME (:0)",
            sql.Text);
        Assert.Equal([1], sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void QuantifiedExpression_ArrayConstructorOperand_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Name == Any(Array("a", "b")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".name = ANY (ARRAY[:0, :1])",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    // --- Guards -----------------------------------------------------------------

    [Fact]
    public void BindArray_NullArray_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            BindArray<int>((int[])null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'values')",
            ex.Message);
    }

    [Fact]
    public void BindArray_NullCollection_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            BindArray((List<int>)null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'values')",
            ex.Message);
    }

    [Fact]
    public void BindArray_UnbindableElementType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            BindArray(new object[] { 1 }));

        Assert.Equal("Invalid element type for BindArray: System.Object", ex.Message);
    }
}
