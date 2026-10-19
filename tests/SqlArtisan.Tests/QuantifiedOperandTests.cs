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

    // PostgreSQL 16.15 reads COUNT(ALL (x)) as the aggregate's own ALL, so the text runs
    // as another construct; Count(x) spells what it computes.
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
            "The SET assignment list cannot take ANY, ALL or SOME; assign a value or a "
                + "scalar subquery.",
            ex.Message);
    }

    [Fact]
    public void Any_OnDuplicateKeyUpdateAssignment_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(_t, _t.Code)
                .Values(1)
                .OnDuplicateKeyUpdate(_t.Code == Any(Select(_t.Code).From(_t))));

        Assert.Equal(
            "The ON DUPLICATE KEY UPDATE assignment list cannot take ANY, ALL or SOME; "
                + "assign a value or a scalar subquery.",
            ex.Message);
    }

    [Fact]
    public void Any_JsonbExistsAnyKeys_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => JsonbExistsAny(_t.Name, Any(BindArray(["a"]))));

        Assert.Equal(
            "ANY requires a comparison; use it as the right operand of one, as in "
                + "col == Any(...).",
            ex.Message);
    }

    // Off SqlExpression, a string beside one would compile as C# concatenation and bind
    // its type name, so both orders are compile errors.
    [Theory]
    [InlineData(typeof(SqlArtisan.Internal.QuantifiedExpression), true)]
    [InlineData(typeof(SqlArtisan.Internal.QuantifiedExpression), false)]
    [InlineData(typeof(SqlArtisan.Internal.QuantifiedSubquery), true)]
    [InlineData(typeof(SqlArtisan.Internal.QuantifiedSubquery), false)]
    public void Quantified_AdditionBesideAString_IsACompileError(Type type, bool stringFirst)
    {
        System.Reflection.MethodInfo op = type.GetMethod(
            "op_Addition",
            stringFirst ? [typeof(string), type] : [type, typeof(string)])!;
        ObsoleteAttribute obsolete = Assert.Single(
            op.GetCustomAttributes(typeof(ObsoleteAttribute), false).Cast<ObsoleteAttribute>());

        Assert.True(obsolete.IsError);
        Assert.Equal(
            "ANY, ALL or SOME cannot be added to a string; use it as the right operand of a "
                + "comparison, as in col == Any(...).",
            obsolete.Message);
    }

    [Fact]
    public void Any_LikeEscape_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Name.Like(Any(BindArray(["a!%"]))).Escape('!'));

        Assert.Equal(
            "LIKE ANY, ALL or SOME takes no ESCAPE; write one LIKE ... ESCAPE per pattern.",
            ex.Message);
    }

    [Fact]
    public void All_NotLikeEscape_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Name.NotLike(All(Select(_t.Name).From(_t))).Escape('!'));

        Assert.Equal(
            "NOT LIKE ANY, ALL or SOME takes no ESCAPE; write one NOT LIKE ... ESCAPE per "
                + "pattern.",
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
