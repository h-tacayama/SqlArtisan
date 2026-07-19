using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ArrayBindTests
{
    private readonly TestTable _t = new("t");

    // --- ArrayBind + ANY --------------------------------------------------------

    [Fact]
    public void ArrayBind_EqualsAny_CorrectSql()
    {
        int[] values = [1, 2, 3];

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == Any(ArrayBind(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Same(values, sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void ArrayBind_Collection_BindsSingleParameter()
    {
        List<string> values = ["a", "b"];

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Name == Any(ArrayBind(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".name = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(["a", "b"], sql.Parameters.Get<string[]>(":0"));
    }

    [Fact]
    public void ArrayBind_EmptyArray_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == Any(ArrayBind(System.Array.Empty<int>())))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Empty(sql.Parameters.Get<int[]>(":0")!);
    }

    [Fact]
    public void ArrayBind_NullableElements_CorrectSql()
    {
        int?[] values = [1, null];

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == Any(ArrayBind(values)))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".code = ANY (:0)",
            sql.Text);
        Assert.Same(values, sql.Parameters.Get<int?[]>(":0"));
    }

    [Fact]
    public void ArrayBind_SharedInstance_ReusesMarker()
    {
        ArrayBindValue shared = ArrayBind([1, 2]);

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
            .Where(_t.Code < All(ArrayBind([10, 20])))
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
            .Where(_t.Code == Some(ArrayBind([1])))
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
    public void ArrayBind_NullArray_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ArrayBind<int>((int[])null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'values')",
            ex.Message);
    }

    [Fact]
    public void ArrayBind_NullCollection_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ArrayBind((List<int>)null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'values')",
            ex.Message);
    }

    [Fact]
    public void ArrayBind_UnbindableElementType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ArrayBind(new object[] { 1 }));

        Assert.Equal("Invalid element type for ArrayBind: System.Object", ex.Message);
    }
}
