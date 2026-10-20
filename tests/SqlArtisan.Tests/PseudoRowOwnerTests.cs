using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// EXCLUDED / INSERTED / DELETED drop their column's owner, so another table's column
// would read the target's same-named one; Build() rejects it instead.
public class PseudoRowOwnerTests
{
    private const string ExcludedMessage =
        "EXCLUDED reads only the target table's row; pass one of its columns, or read "
            + "another table's column directly.";

    private readonly TestTable _t = new("t");
    private readonly ArchiveTable _a = new("a");

    [Fact]
    public void Excluded_OtherTableInDoUpdateSet_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(_t, _t.Code, _t.Name)
                .Values(1, "x")
                .OnConflict(_t.Code)
                .DoUpdateSet(_t.Name == Excluded(_a.Name))
                .Build());

        Assert.Equal(ExcludedMessage, ex.Message);
    }

    [Fact]
    public void Excluded_OtherTableInDoUpdateWhere_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(_t, _t.Code, _t.Name)
                .Values(1, "x")
                .OnConflict(_t.Code)
                .DoUpdateSet(_t.Name == Excluded(_t.Name))
                .Where(Excluded(_a.Code) > 0)
                .Build());

        Assert.Equal(ExcludedMessage, ex.Message);
    }

    [Fact]
    public void Excluded_MySql_OtherTableInOnDuplicateKeyUpdate_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(t, t.Code, t.Name)
                .Values(1, "x")
                .OnDuplicateKeyUpdate(t.Name == Excluded(_a.Name))
                .Build(Dbms.MySql));

        Assert.Equal(ExcludedMessage, ex.Message);
    }

    // A subquery inside the update clause still reads the statement's EXCLUDED row.
    [Fact]
    public void Excluded_OtherTableInUpdateClauseSubquery_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(_t, _t.Code, _t.Name)
                .Values(1, "x")
                .OnConflict(_t.Code)
                .DoUpdateSet(_t.Name == Select(Max(Excluded(_a.Name))).From(_a))
                .Build());

        Assert.Equal(ExcludedMessage, ex.Message);
    }

    // The table name decides, as for a SET target (#674): another handle of the target
    // table reads the same EXCLUDED row.
    [Fact]
    public void Excluded_SameTableOtherHandle_CorrectSql()
    {
        TestTable other = new("o");

        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "x")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(other.Name))
            .Build();

        Assert.Equal(
            "INSERT INTO test_table AS \"t\" (code, name) VALUES (:0, :1) "
                + "ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("x", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Inserted_SqlServer_JoinedTableInOutput_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Update(_t)
                .Set(_t.Name == _a.Name)
                .Output(Inserted(_a.Name))
                .From(_t)
                .InnerJoin(_a).On(_t.Code == _a.Code)
                .Build(Dbms.SqlServer));

        Assert.Equal(
            "INSERTED reads only the target table's row; pass one of its columns, or read "
                + "another table's column directly.",
            ex.Message);
    }

    [Fact]
    public void Inserted_SqlServer_OtherTableInsideOutputFunction_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(t, t.Code)
                .Output(Coalesce(Inserted(_a.Code), 0))
                .Values(1)
                .Build(Dbms.SqlServer));

        Assert.Equal(
            "INSERTED reads only the target table's row; pass one of its columns, or read "
                + "another table's column directly.",
            ex.Message);
    }

    [Fact]
    public void Deleted_SqlServer_OtherTableInOutput_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => DeleteFrom(t)
                .Output(Deleted(_a.Code))
                .Build(Dbms.SqlServer));

        Assert.Equal(
            "DELETED reads only the target table's row; pass one of its columns, or read "
                + "another table's column directly.",
            ex.Message);
    }
}
