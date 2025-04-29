using System.Text;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class UpdateTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Update_SetLiterals_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == 1,
                _t.Name == "a",
                _t.CreatedAt == SysDate)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("\"t\".code = :0, ");
        expected.Append("\"t\".name = :1, ");
        expected.Append("\"t\".created_at = SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SetMultipleVariables_CorrectSql()
    {
        TestTableDto dto = new(1, "Test1", new DateTime(2001, 2, 3));

        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == dto.Code,
                _t.Name == dto.Name,
                _t.CreatedAt == dto.CreatedAt)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("\"t\".code = :0, ");
        expected.Append("\"t\".name = :1, ");
        expected.Append("\"t\".created_at = :2");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Test1", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
    }

    [Fact]
    public void Update_SetWithInequality_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Update(_t)
            .Set(
                _t.Code != 1)
            .Build();
        });
    }
}
