namespace SqlArtisan.Tests;

public class ComparisonTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public ComparisonTests()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void Equal_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code == 2, "\"t\".code = :0", 1, 2);

    [Fact]
    public void NotEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code != 2, "\"t\".code <> :0", 1, 2);

    [Fact]
    public void LessThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code < 2, "\"t\".code < :0", 1, 2);

    [Fact]
    public void GreaterThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code > 2, "\"t\".code > :0", 1, 2);

    [Fact]
    public void LessEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code <= 2, "\"t\".code <= :0", 1, 2);

    [Fact]
    public void GreaterEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.Code >= 2, "\"t\".code >= :0", 1, 2);

    // A C# null here is a nullable variable meant as a NULL test; `= NULL` is
    // unknown, so the message names IS NULL — and, for `=`, the SET remedy too.
    [Theory]
    [InlineData(
        "op_Equality",
        "= cannot take a C# null; test for NULL with .IsNull, or write Sql.Null to assign NULL.")]
    [InlineData(
        "op_Inequality",
        "<> cannot compare a C# null; test for NULL with .IsNull or .IsNotNull.")]
    [InlineData(
        "op_LessThan",
        "< cannot compare a C# null; test for NULL with .IsNull or .IsNotNull.")]
    [InlineData(
        "op_GreaterThan",
        "> cannot compare a C# null; test for NULL with .IsNull or .IsNotNull.")]
    [InlineData(
        "op_LessThanOrEqual",
        "<= cannot compare a C# null; test for NULL with .IsNull or .IsNotNull.")]
    [InlineData(
        "op_GreaterThanOrEqual",
        ">= cannot compare a C# null; test for NULL with .IsNull or .IsNotNull.")]
    public void Comparison_NullRightOperand_ThrowsArgumentNullException(
        string operatorName,
        string message)
    {
        System.Reflection.MethodInfo op = typeof(SqlExpression).GetMethod(
            operatorName, [typeof(SqlExpression), typeof(object)])!;

        System.Reflection.TargetInvocationException wrapped =
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                op.Invoke(null, [_t.Code, null]));
        ArgumentNullException ex = Assert.IsType<ArgumentNullException>(wrapped.InnerException);

        Assert.Equal($"{message} (Parameter 'rightSide')", ex.Message);
    }
}
