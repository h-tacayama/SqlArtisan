using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Decode_WithIntDefault_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    [
                        (1, 10),
                        (2, 20),
                    ],
                    999))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Decode_WithDoubleDefault_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    [
                        (1, Null),
                        (Null, 20),
                    ],
                    999.9))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, NULL, NULL, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
