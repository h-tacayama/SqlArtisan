using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class MultiRowInsertTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Values_TwoRows_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1), (:2, :3)");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .Build();

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Values_ThreeRows_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1), (:2, :3), (:4, :5)");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .Values(3, "c")
            .Build();

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Values_SingleRow_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1)");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Build();

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Values_MultiRowWithReturning_CorrectSql()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1), (:2, :3) ");
        expected.Append("RETURNING code, name");

        // Act
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .Returning(_t.Code, _t.Name)
            .Build();

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }
}
