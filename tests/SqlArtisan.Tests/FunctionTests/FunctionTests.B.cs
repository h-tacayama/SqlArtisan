using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Bind_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Bind(5))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(5, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Bind_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(Bind(5))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("?0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Bind_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Bind(5))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("@0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // Two separate Bind(...) calls, even with an equal value, mint distinct
    // markers — reuse requires sharing the same handle instance (#241).
    [Fact]
    public void Bind_CalledTwiceWithEqualValue_CreatesDistinctMarkers()
    {
        SqlStatement sql =
            Select(Bind(1), Bind(1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0, :1");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
    }

    // Sharing a Bind(...) handle across clauses reuses its marker in each — the
    // #241 mechanism this factory exposes directly.
    [Fact]
    public void Bind_SharedHandleInSelectAndGroupBy_CorrectSql()
    {
        BindValue p10 = Bind(10);
        BindValue low = Bind("Low");
        BindValue other = Bind("Other");

        SqlStatement sql =
            Select(Case(_t.Code, When(p10).Then(low), Else(other)))
            .From(_t)
            .GroupBy(Case(_t.Code, When(p10).Then(low), Else(other)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE \"t\".code WHEN :0 THEN :1 ELSE :2 END ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CASE \"t\".code WHEN :0 THEN :1 ELSE :2 END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
        Assert.Equal(10, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Low", sql.Parameters.Get<string>(":1"));
        Assert.Equal("Other", sql.Parameters.Get<string>(":2"));
    }

    [Fact]
    public void Bind_NullValue_ThrowsArgumentNullException()
    {
        ArgumentNullException ex =
            Assert.Throws<ArgumentNullException>(() => Bind(null!));

        Assert.Equal(
            "Value cannot be null. Use Sql.Null to represent SQL NULL. (Parameter 'value')",
            ex.Message);
    }

    [Fact]
    public void Bind_NonBindableValue_ThrowsArgumentException()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Bind(new object()));

        Assert.Equal("Invalid type for Bind: System.Object", ex.Message);
    }

    [Fact]
    public void Bind_SqlExpressionValue_ThrowsArgumentException()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Bind(_t.Code));

        Assert.Equal("Invalid type for Bind: SqlArtisan.DbColumn", ex.Message);
    }

    [Fact]
    public void BindNull_NoArguments_CorrectSql()
    {
        SqlStatement sql =
            Select(BindNull())
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void BindNull_WithDbType_SetsDbType()
    {
        BindValue bind = BindNull(System.Data.DbType.Int32);

        Assert.Equal(DBNull.Value, bind.Value);
        Assert.Equal(System.Data.DbType.Int32, bind.DbType);
    }
}
