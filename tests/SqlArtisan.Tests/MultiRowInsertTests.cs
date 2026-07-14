using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class MultiRowInsertTests
{
    private readonly TestTable _t = new();

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

    [Fact]
    public void Values_MismatchedRowLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2));
    }

    [Fact]
    public void Values_RowCollection_MatchesChainedCalls()
    {
        List<object[]> rows =
        [
            [1, "a"],
            [2, "b"],
            [3, "c"],
        ];

        SqlStatement collection =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(rows)
            .Build();

        SqlStatement chained =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .Values(3, "c")
            .Build();

        // The collection-driven form is exactly the chained form — same SQL, same binds.
        Assert.Equal(chained.Text, collection.Text);
        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1), (:2, :3), (:4, :5)",
            collection.Text);
        Assert.Equal(chained.Parameters.Count, collection.Parameters.Count);
    }

    [Fact]
    public void Values_JaggedArrayFromSelect_CorrectSql()
    {
        // A .Select(...).ToArray() yields object[][]; the typed-array overload
        // keeps it from being ambiguous with the params object[] form.
        object[][] rows = [.. Enumerable.Range(1, 3).Select(i => new object[] { i, $"n{i}" })];

        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(rows)
            .Build();

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (:0, :1), (:2, :3), (:4, :5)",
            sql.Text);
        Assert.Equal(6, sql.Parameters.Count);
    }

    [Fact]
    public void Values_LargeRowCollection_MatchesChainedParameterCount()
    {
        const int rowCount = 500;

        List<object[]> rows = [.. Enumerable.Range(0, rowCount).Select(i => new object[] { i, $"name{i}" })];

        SqlStatement collection =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(rows)
            .Build();

        var chained = InsertInto(_t, _t.Code, _t.Name).Values(0, "name0");
        for (int i = 1; i < rowCount; i++)
        {
            chained = chained.Values(i, $"name{i}");
        }
        SqlStatement chainedSql = chained.Build();

        Assert.Equal(chainedSql.Text, collection.Text);
        Assert.Equal(rowCount * 2, collection.Parameters.Count);
    }

    [Fact]
    public void Values_EmptyRowCollection_ThrowsArgumentException()
    {
        List<object[]> empty = [];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(_t, _t.Code, _t.Name).Values(empty));

        Assert.Equal(
            "VALUES requires at least one row; the row collection is empty.",
            ex.Message);
    }

    [Fact]
    public void Values_RowCollectionMismatchedWidth_ThrowsArgumentException()
    {
        List<object[]> rows =
        [
            [1, "a"],
            [2],
        ];

        // Per-row width validation reuses the existing multi-row guard.
        Assert.Throws<ArgumentException>(() =>
            InsertInto(_t, _t.Code, _t.Name).Values(rows));
    }
}
