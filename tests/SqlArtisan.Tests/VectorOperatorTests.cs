using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class VectorOperatorTests
{
    private readonly TestTable _t = new("t");

    // --- L2Distance (<->) -------------------------------------------------------

    [Fact]
    public void L2Distance_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(L2Distance(Cast("[1,2,3]", "vector"), Cast("[4,5,6]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS vector) <-> CAST(:1 AS vector))", sql.Text);
        Assert.Equal("[1,2,3]", sql.Parameters.Get<string>(":0"));
        Assert.Equal("[4,5,6]", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void L2Distance_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(L2Distance(Cast("[1,2,3]", "vector"), Cast("[4,5,6]", "vector")))
            .Build(Dbms.MySql);

        Assert.Equal("SELECT (CAST(?0 AS vector) <-> CAST(?1 AS vector))", sql.Text);
        Assert.Equal("[1,2,3]", sql.Parameters.Get<string>("?0"));
        Assert.Equal("[4,5,6]", sql.Parameters.Get<string>("?1"));
    }

    [Fact]
    public void L2Distance_ColumnOperand_CorrectSql()
    {
        SqlStatement sql =
            Select(L2Distance(_t.Name, Cast("[1,2,3]", "vector")))
            .From(_t)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT (\"t\".name <-> CAST(:0 AS vector)) FROM test_table \"t\"",
            sql.Text);
        Assert.Equal("[1,2,3]", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void L2Distance_InOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(L2Distance(_t.Name, Cast("[1,2,3]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" ORDER BY (\"t\".name <-> CAST(:0 AS vector))",
            sql.Text);
        Assert.Equal("[1,2,3]", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void L2Distance_Nested_CorrectSql()
    {
        SqlStatement sql =
            Select(L2Distance(
                L2Distance(Cast("[1]", "vector"), Cast("[2]", "vector")),
                Cast("[3]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT ((CAST(:0 AS vector) <-> CAST(:1 AS vector)) <-> CAST(:2 AS vector))",
            sql.Text);
        Assert.Equal("[1]", sql.Parameters.Get<string>(":0"));
        Assert.Equal("[2]", sql.Parameters.Get<string>(":1"));
        Assert.Equal("[3]", sql.Parameters.Get<string>(":2"));
    }

    // --- CosineDistance (<=>) ---------------------------------------------------

    [Fact]
    public void CosineDistance_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(CosineDistance(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS vector) <=> CAST(:1 AS vector))", sql.Text);
        Assert.Equal("[1,2]", sql.Parameters.Get<string>(":0"));
        Assert.Equal("[3,4]", sql.Parameters.Get<string>(":1"));
    }

    // --- NegativeInnerProduct (<#>) ---------------------------------------------

    [Fact]
    public void NegativeInnerProduct_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(NegativeInnerProduct(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS vector) <#> CAST(:1 AS vector))", sql.Text);
        Assert.Equal("[1,2]", sql.Parameters.Get<string>(":0"));
        Assert.Equal("[3,4]", sql.Parameters.Get<string>(":1"));
    }

    // --- L1Distance (<+>) -------------------------------------------------------

    [Fact]
    public void L1Distance_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(L1Distance(Cast("[1,2]", "vector"), Cast("[3,4]", "vector")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS vector) <+> CAST(:1 AS vector))", sql.Text);
        Assert.Equal("[1,2]", sql.Parameters.Get<string>(":0"));
        Assert.Equal("[3,4]", sql.Parameters.Get<string>(":1"));
    }

    // --- HammingDistance (<~>) --------------------------------------------------

    [Fact]
    public void HammingDistance_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(HammingDistance(Cast("101", "bit(3)"), Cast("111", "bit(3)")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS bit(3)) <~> CAST(:1 AS bit(3)))", sql.Text);
        Assert.Equal("101", sql.Parameters.Get<string>(":0"));
        Assert.Equal("111", sql.Parameters.Get<string>(":1"));
    }

    // --- JaccardDistance (<%>) --------------------------------------------------

    [Fact]
    public void JaccardDistance_CastOperands_CorrectSql()
    {
        SqlStatement sql =
            Select(JaccardDistance(Cast("101", "bit(3)"), Cast("111", "bit(3)")))
            .Build(Dbms.PostgreSql);

        Assert.Equal("SELECT (CAST(:0 AS bit(3)) <%> CAST(:1 AS bit(3)))", sql.Text);
        Assert.Equal("101", sql.Parameters.Get<string>(":0"));
        Assert.Equal("111", sql.Parameters.Get<string>(":1"));
    }
}
