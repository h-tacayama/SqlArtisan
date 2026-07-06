using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// Demonstrates the #244 fix: SqlCondition/SqlExpression are named here with no
// `using SqlArtisan.Internal;` — both are root-namespace public types.
public class DynamicConditionAccumulatorTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Fold_AccumulatedInLoop_CorrectSql()
    {
        SqlCondition where = _t.Code > 0;
        int[] excludedCodes = [10, 20];
        foreach (int code in excludedCodes)
        {
            where &= _t.Code != code;
        }

        SqlStatement sql = Select(_t.Name).From(_t).Where(where).Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".name ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE (\"t\".code > :0) AND (\"t\".code <> :1) AND (\"t\".code <> :2)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Count);
    }

    [Fact]
    public void HelperMethodReturningSqlCondition_CorrectSql()
    {
        static SqlCondition CodeFilter(TestTable t, int? code) =>
            code is null ? t.Code > 0 : t.Code == code.Value;

        SqlStatement sql = Select(_t.Name).From(_t).Where(CodeFilter(_t, 5)).Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".name ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE \"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void HelperMethodReturningSqlExpression_CorrectSql()
    {
        static SqlExpression Discounted(TestTable t) => t.Code * 0.9;

        SqlStatement sql = Select(Discounted(_t)).From(_t).Build();

        StringBuilder expected = new();
        expected.Append("SELECT (\"t\".code * :0) ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
