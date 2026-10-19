using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// ANY / ALL / SOME complete a comparison only as its right operand, so every other
// position rejects them at the call (#666).
public class QuantifiedOperandTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Any_SelectItem_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Select(Any(BindArray([1]))));

        Assert.Equal(
            "ANY requires a comparison; use it as the right operand of one, as in "
                + "col == Any(...).",
            ex.Message);
    }

    // PostgreSQL reads COUNT(ALL (x)) as the aggregate's own ALL, so the text runs as
    // another construct; Count(x) spells what it computes.
    [Fact]
    public void All_AggregateArgument_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Count(All(BindArray([1]))));

        Assert.Equal(
            "ALL requires a comparison; use it as the right operand of one, as in "
                + "col == All(...).",
            ex.Message);
    }

    [Fact]
    public void Some_InList_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Code.In(Some(BindArray([1]))));

        Assert.Equal(
            "SOME requires a comparison; use it as the right operand of one, as in "
                + "col == Some(...).",
            ex.Message);
    }

    [Fact]
    public void Any_Between_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Code.Between(Any(BindArray([1])), 2));

        Assert.Equal(
            "ANY requires a comparison; use it as the right operand of one, as in "
                + "col == Any(...).",
            ex.Message);
    }

    [Fact]
    public void Any_ArrayContainsLeftOperand_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => ArrayContains(Any(BindArray([1])), _t.Code));

        Assert.Equal(
            "ANY requires a comparison; use it as the right operand of one, as in "
                + "col == Any(...).",
            ex.Message);
    }

    [Fact]
    public void Any_RegexpLikePattern_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => RegexpLike(_t.Name, Any(BindArray(["a"]))));

        Assert.Equal(
            "ANY requires a comparison; use it as the right operand of one, as in "
                + "col == Any(...).",
            ex.Message);
    }

    [Fact]
    public void Any_SetAssignment_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Update(_t).Set(_t.Code == Any(Select(_t.Code).From(_t))));

        Assert.Equal(
            "A SET assignment cannot take ANY, ALL or SOME; assign a value or a scalar "
                + "subquery.",
            ex.Message);
    }

    [Fact]
    public void All_NotEqual_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code != All(BindArray([1, 2])))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code <> ALL (:0)", sql.Text);
        Assert.Equal([1, 2], sql.Parameters.Get<int[]>(":0"));
    }

    [Fact]
    public void Any_LikePattern_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Name.Like(Any(BindArray(["a%", "b%"]))))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name LIKE ANY (:0)",
            sql.Text);
        Assert.Equal(["a%", "b%"], sql.Parameters.Get<string[]>(":0"));
    }

    [Fact]
    public void Any_ArrayContainsRightOperand_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(ArrayContains(_t.Name, Any(Select(_t.Name).From(_t))))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name @> ANY "
                + "(SELECT \"t\".name FROM test_table \"t\")",
            sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }

    [Fact]
    public void Any_JsonbExistsKey_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(JsonbExists(_t.Name, Any(BindArray(["a"]))))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name ? ANY (:0)", sql.Text);
        Assert.Equal(["a"], sql.Parameters.Get<string[]>(":0"));
    }

    [Fact]
    public void Any_TsMatchQuery_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(TsMatch(_t.Name, Any(Select(_t.Name).From(_t))))
            .Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".name @@ ANY "
                + "(SELECT \"t\".name FROM test_table \"t\")",
            sql.Text);
        Assert.Equal(0, sql.Parameters.Count);
    }
}
