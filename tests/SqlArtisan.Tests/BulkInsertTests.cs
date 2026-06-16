using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class BulkInsertTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void BuildBatches_FitsUnderCap_ReturnsSingleStatement()
    {
        // Arrange (default DBMS is PostgreSQL, cap 65535 — three rows never split)
        string expected = ExpectedBatch(rowCount: 3, marker: ':', suffix: "");

        // Act
        IReadOnlyList<SqlStatement> batches =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt)
            .Values(0, "n0", 0)
            .Values(1, "n1", 1)
            .Values(2, "n2", 2)
            .BuildBatches();

        // Assert
        Assert.Single(batches);
        Assert.Equal(expected, batches[0].Text);
        Assert.Equal(9, batches[0].Parameters.Count);
    }

    [Fact]
    public void BuildBatches_ExceedsCap_SplitsAtParameterBoundary()
    {
        // Arrange: SQLite caps parameters at 999; with three columns per row that
        // is 333 rows per batch. 667 rows therefore split into 333 / 333 / 1, and
        // the ON CONFLICT tail must repeat on every batch.
        const int rowCount = 667;
        const string suffix = "ON CONFLICT (code) DO NOTHING";
        string fullBatch = ExpectedBatch(333, ':', suffix);
        string tailBatch = ExpectedBatch(1, ':', suffix);

        IInsertBuilderValues builder =
            InsertInto(_t, _t.Code, _t.Name, _t.CreatedAt).Values(0, "n0", 0);
        for (int i = 1; i < rowCount; i++)
        {
            builder = builder.Values(i, $"n{i}", i);
        }

        // Act
        IReadOnlyList<SqlStatement> batches =
            builder.OnConflict(_t.Code).DoNothing().BuildBatches(Dbms.Sqlite);

        // Assert
        Assert.Equal(3, batches.Count);
        Assert.Equal(fullBatch, batches[0].Text);
        Assert.Equal(fullBatch, batches[1].Text);
        Assert.Equal(tailBatch, batches[2].Text);
        Assert.Equal(999, batches[0].Parameters.Count);
        Assert.Equal(999, batches[1].Parameters.Count);
        Assert.Equal(3, batches[2].Parameters.Count);
    }

    [Fact]
    public void BuildBatches_WithDoUpdateSet_ReplicatesUpsertClause()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name) ");
        expected.Append("VALUES (:0, :1), (:2, :3) ");
        expected.Append("ON CONFLICT (code) ");
        expected.Append("DO UPDATE SET name = excluded.name");

        // Act
        IReadOnlyList<SqlStatement> batches =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .BuildBatches(Dbms.Sqlite);

        // Assert
        Assert.Single(batches);
        Assert.Equal(expected.ToString(), batches[0].Text);
    }

    [Fact]
    public void BuildBatches_Oracle_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Values(2, "b")
            .BuildBatches(Dbms.Oracle));
    }

    // Builds the exact text of one batch. Parameter indices restart at zero per
    // batch because each batch is built independently.
    private static string ExpectedBatch(int rowCount, char marker, string suffix)
    {
        StringBuilder sb = new();
        sb.Append("INSERT INTO test_table (code, name, created_at) VALUES ");

        int p = 0;
        for (int r = 0; r < rowCount; r++)
        {
            if (r > 0)
            {
                sb.Append(", ");
            }

            sb.Append('(')
                .Append(marker).Append(p++).Append(", ")
                .Append(marker).Append(p++).Append(", ")
                .Append(marker).Append(p++)
                .Append(')');
        }

        if (suffix.Length > 0)
        {
            sb.Append(' ').Append(suffix);
        }

        return sb.ToString();
    }
}
