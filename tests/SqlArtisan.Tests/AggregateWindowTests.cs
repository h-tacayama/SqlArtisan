using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class AggregateWindowTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Over_NullPartitionBy_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Sum(t.Code).Over((SqlArtisan.PartitionByClause)null!));

        // ParamName is the public Over(...) parameter, not an internal name.
        Assert.Equal("partitionByClause", ex.ParamName);
        Assert.Equal("Value cannot be null. (Parameter 'partitionByClause')", ex.Message);
    }

    [Fact]
    public void Sum_OverEmpty_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER ()";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over()).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Sum_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    // #673: bound, each would partition by a constant.
    public static TheoryData<object, string> BoundPartitionValues => new()
    {
        { 1, "System.Int32" },
        { "code", "System.String" },
        { DayOfWeek.Monday, "System.DayOfWeek" },
    };

    [Theory]
    [MemberData(nameof(BoundPartitionValues))]
    public void PartitionBy_BoundValue_ThrowsArgumentException(object key, string typeName)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => PartitionBy(key));

        Assert.Equal($"Invalid type for PartitionByItem: {typeName}", ex.Message);
    }

    [Fact]
    public void PartitionBy_ExplicitBind_CorrectSql()
    {
        SqlStatement sql = Select(Sum(_t.Code).Over(PartitionBy(Bind("a")))).Build();

        Assert.Equal("SELECT SUM(code) OVER (PARTITION BY :0)", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void PartitionBy_NullKey_ThrowsArgumentNullException()
    {
        // Sql.Null, the generic remedy, would partition by a constant too.
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            PartitionBy(_t.Code, null!));

        Assert.Equal(
            "A PARTITION BY key cannot be a C# null; pass a column or an expression. "
                + "(Parameter 'expressions')",
            ex.Message);
    }

    [Fact]
    public void Sum_OverOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (ORDER BY code)";

        // Act
        SqlStatement sql = Select(Sum(_t.Code).Over(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Sum_OverPartitionByOrderBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (PARTITION BY name ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(PartitionBy(_t.Name).OrderBy(_t.Code)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Count_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT COUNT(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Count(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Count_Asterisk_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT COUNT(*) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Count(Asterisk).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Avg_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT AVG(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Avg(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Max_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT MAX(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Max(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Min_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT MIN(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(Min(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void StddevPop_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT STDDEV_POP(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(StddevPop(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void VarSamp_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT VAR_SAMP(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql = Select(VarSamp(_t.Code).Over(PartitionBy(_t.Name))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Stdev_SqlServer_OverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected = "SELECT STDEV(code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql =
            Select(Stdev(_t.Code).Over(PartitionBy(_t.Name)))
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PartitionBy_WithNoExpressions_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PartitionBy());
    }

    [Fact]
    public void PartitionBy_WithNullExpressions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PartitionBy(null!));
    }
}
