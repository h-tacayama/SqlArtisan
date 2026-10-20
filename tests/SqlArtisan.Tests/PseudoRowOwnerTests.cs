using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// EXCLUDED / INSERTED / DELETED drop their column's owner, so another table's column
// would read the target's same-named one; Build() rejects it instead.
public class PseudoRowOwnerTests
{
    private const string ExcludedMessage =
        "EXCLUDED must name a column of the INSERT target table; read another table's "
            + "value through a subquery.";

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

        Assert.Equal(
            "new must name a column of the INSERT target table; read another table's value "
                + "through a subquery.",
            ex.Message);
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

    // The remedy the message names: another table's value read through a subquery.
    [Fact]
    public void Excluded_TargetColumnInUpdateClauseSubquery_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "x")
            .OnConflict(_t.Code)
            .DoUpdateSet(
                _t.Name == Select(_a.Name).From(_a).Where(_a.Code == Excluded(_t.Code)))
            .Build();

        Assert.Equal(
            "INSERT INTO test_table AS \"t\" (code, name) VALUES (:0, :1) "
                + "ON CONFLICT (code) DO UPDATE SET name = (SELECT \"a\".name "
                + "FROM archive_table \"a\" WHERE \"a\".code = EXCLUDED.code)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("x", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Excluded_MySql_TargetColumnInUpdateClauseSubquery_CorrectSql()
    {
        TestTable t = new();

        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "x")
            .OnDuplicateKeyUpdate(
                t.Name == Select(_a.Name).From(_a).Where(_a.Code == Excluded(t.Code)))
            .Build(Dbms.MySql);

        Assert.Equal(
            "INSERT INTO test_table (code, name) VALUES (?0, ?1) AS new "
                + "ON DUPLICATE KEY UPDATE name = (SELECT `a`.name "
                + "FROM archive_table `a` WHERE `a`.code = new.code)",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>("?0"));
        Assert.Equal("x", sql.Parameters.Get<string>("?1"));
    }

    // A data-modifying CTE reads its own target, and the outer statement's comes back.
    [Fact]
    public void Excluded_AfterDataModifyingCte_CorrectSql()
    {
        TestCte cte = new("c");

        SqlStatement sql =
            With(cte.As(InsertInto(_a, _a.Code).Select(_t.Code).From(_t)))
            .InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "x")
            .OnConflict(_t.Code)
            .DoUpdateSet(_t.Name == Excluded(_t.Name))
            .Build();

        Assert.Equal(
            "WITH \"c\" AS (INSERT INTO archive_table AS \"a\" (code) "
                + "SELECT \"t\".code FROM test_table \"t\") "
                + "INSERT INTO test_table AS \"t\" (code, name) VALUES (:0, :1) "
                + "ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("x", sql.Parameters.Get<string>(":1"));
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
            "INSERTED must name a column of the statement's target table.",
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
            "INSERTED must name a column of the statement's target table.",
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
            "DELETED must name a column of the statement's target table.",
            ex.Message);
    }
}
