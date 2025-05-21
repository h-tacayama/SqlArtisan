using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

internal sealed class ConditionTestAssert(TestTable t)
{
    private readonly TestTable _t = t;

    internal void Equal(
        SqlCondition testCondition,
        string expectedSql,
        int expectedBindCount = 0,
        params object[] bindValues)
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(testCondition)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append(expectedSql);

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(expectedBindCount, sql.Parameters.Count);

        for (int i = 0; i < bindValues.Length; i++)
        {
            Assert.Equal(bindValues[i], sql.Parameters.Get<object>($":{i}"));
        }
    }
}
