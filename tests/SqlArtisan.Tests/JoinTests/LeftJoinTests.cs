using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class LeftJoinTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void LeftJoin_SimpleCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .LeftJoin(_s)
            .On(_t.Code == _s.Code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("LEFT JOIN ");
        expected.Append("test_table \"s\" ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LeftJoin_OnAllConditionsExcluded_ThrowsArgumentException()
    {
        // LEFT/RIGHT/FULL joins share the single OnClause guard with INNER JOIN.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code)
            .From(_t)
            .LeftJoin(_s)
            .On(ConditionIf(false, _t.Code == _s.Code))
            .Build());

        Assert.Equal(
            "A JOIN's ON clause requires a condition; use CrossJoin for an unconditional join.",
            ex.Message);
    }
}
