using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

internal sealed class ConditionTestAssert(test_table t)
{
    private readonly test_table _t = t;

    internal void Equal(
        AbstractCondition testCondition,
        string expectedSql,
        int expectedBindCount = 0,
        params object[] bindValues)
    {
        SqlStatement sql =
            SELECT(_t.name)
            .FROM(_t)
            .WHERE(testCondition)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append(expectedSql);

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(expectedBindCount, sql.ParameterCount);

        for (int i = 0; i < bindValues.Length; i++)
        {
            Assert.Equal(bindValues[i], sql.Parameters.Get<object>($":{i}"));
        }
    }
}
