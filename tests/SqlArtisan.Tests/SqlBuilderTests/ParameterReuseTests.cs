using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// #241 (GAP-19): the same expression instance formatted more than once reuses
// its bind markers, so engines that match GROUP BY syntactically accept it.
public class ParameterReuseTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Build_SharedExpressionInstanceInSelectAndGroupBy_CorrectSql()
    {
        SqlExpression label =
            Case(
                _t.Code,
                When(1).Then("Low"),
                Else("Other"));

        SqlStatement sql =
            Select(label)
            .From(_t)
            .GroupBy(label)
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
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Low", sql.Parameters.Get<string>(":1"));
        Assert.Equal("Other", sql.Parameters.Get<string>(":2"));
    }

    [Fact]
    public void Build_SharedExpressionInstanceTwiceInSelect_CorrectSql()
    {
        SqlExpression label =
            Case(
                _t.Code,
                When(1).Then("Low"),
                Else("Other"));

        SqlStatement sql =
            Select(
                label,
                label)
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE \"t\".code WHEN :0 THEN :1 ELSE :2 END, ");
        expected.Append("CASE \"t\".code WHEN :0 THEN :1 ELSE :2 END ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
    }

    [Fact]
    public void Build_MySql_SharedExpressionInstanceInSelectAndGroupBy_CorrectSql()
    {
        SqlExpression label =
            Case(
                _t.Code,
                When(1).Then("Low"),
                Else("Other"));

        SqlStatement sql =
            Select(label)
            .From(_t)
            .GroupBy(label)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE `t`.code WHEN ?0 THEN ?1 ELSE ?2 END ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("CASE `t`.code WHEN ?0 THEN ?1 ELSE ?2 END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
    }

    [Fact]
    public void Build_SqlServer_SharedExpressionInstanceInSelectAndGroupBy_CorrectSql()
    {
        SqlExpression label =
            Case(
                _t.Code,
                When(1).Then("Low"),
                Else("Other"));

        SqlStatement sql =
            Select(label)
            .From(_t)
            .GroupBy(label)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE \"t\".code WHEN @0 THEN @1 ELSE @2 END ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CASE \"t\".code WHEN @0 THEN @1 ELSE @2 END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
    }

    [Fact] // #282: Sql.Bind(value) exposes the reuse mechanism directly — hoist
           // only the values, write the CASE shape out in both clauses.
    public void Build_SharedBindHandlesInSelectAndGroupBy_CorrectSql()
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

    [Fact] // Reuse is by reference, not value: separately built but identical
           // expressions legitimately mean distinct binds.
    public void Build_DistinctEqualValuedExpressionInstances_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Code,
                    When(1).Then("Low"),
                    Else("Other")))
            .From(_t)
            .GroupBy(
                Case(
                    _t.Code,
                    When(1).Then("Low"),
                    Else("Other")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE \"t\".code WHEN :0 THEN :1 ELSE :2 END ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CASE \"t\".code WHEN :3 THEN :4 ELSE :5 END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(6, sql.Parameters.Count);
    }
}
