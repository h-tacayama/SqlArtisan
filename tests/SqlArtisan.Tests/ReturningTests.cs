using System.Data;
using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ReturningTests
{
    private readonly TestTable _t = new();

    // ── plain RETURNING ────────────────────────────────────────────────

    [Fact]
    public void Returning_OnInsertWithValues_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t, _t.Code, _t.Name)
            .Values(1, "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnInsertWithSet_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Asterisk_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(Asterisk)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING *");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnDelete_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnDeleteWithWhere_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE code = :0 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnUpdate_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_OnUpdateWithWhere_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Where(_t.Code > 0)
            .Returning(_t.Code, _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("WHERE code > :2 ");
        expected.Append("RETURNING code, name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── RETURNING INTO ────────────────────────────────────────────────

    [Fact]
    public void ReturningInto_OnDelete_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnDeleteWithWhere_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE code = :0 ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnInsert_CorrectSql()
    {
        SqlStatement sql =
            InsertInto(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("INSERT INTO ");
        expected.Append("test_table (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ReturningInto_OnUpdate_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(_t.Code == 1, _t.Name == "a")
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table ");
        expected.Append("SET code = :0, name = :1 ");
        expected.Append("RETURNING code, name ");
        expected.Append("INTO :b, :c");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── validation ────────────────────────────────────────────────────

    [Fact]
    public void Returning_NoArguments_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning([])
            .Build());

        Assert.Equal("RETURNING requires at least one expression.", ex.Message);
    }

    [Fact]
    public void Returning_WithNullItem_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, null!));

        Assert.Equal(
            "A C# null is not SQL NULL; pass Sql.Null instead. (Parameter 'returningItem')",
            ex.Message);
    }

    [Theory]
    [InlineData(Dbms.PostgreSql)]
    [InlineData(Dbms.Sqlite)]
    public void Returning_WithExpressionAlias_CorrectSql(Dbms dbms)
    {
        // An alias is valid in a RETURNING list (live-verified on SQLite), so
        // it is emitted faithfully; only the INTO form rejects it.
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code.As("b"))
            .Build(dbms);

        Assert.Equal("DELETE FROM test_table RETURNING code \"b\"", sql.Text);
    }

    [Fact]
    public void ReturningInto_WithExpressionAlias_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code.As("b"))
            .Into(new OutputParameter("b", DbType.Int32)));

        Assert.Equal(
            "RETURNING ... INTO takes no column alias; the output parameter "
            + "names the value, so drop the .As(...) alias.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_DefaultOutputParameter_ThrowsArgumentException()
    {
        // default(OutputParameter) bypasses the constructor guard, so Into
        // must reject it instead of emitting a bare marker.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            InsertInto(_t)
            .Set(_t.Code == 1)
            .Returning(_t.Code)
            .Into(default(OutputParameter)));

        Assert.Equal("An output variable name is required.", ex.Message);
    }

    [Fact]
    public void OutputParameter_WhiteSpaceVariable_ThrowsArgumentException()
    {
        // The variable renders as a bare bind-marker token, so whitespace
        // there is invalid on every dialect.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new OutputParameter(" ", DbType.Int32));

        Assert.Equal("An output variable name is required.", ex.Message);
    }

    [Fact]
    public void OutputParameter_DigitsOnlyVariable_ThrowsArgumentException()
    {
        // Positional binds render as :0, :1, ...; a digit-only name would be
        // reported as a duplicate of one of them.
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new OutputParameter("0", DbType.Int32));

        Assert.Equal(
            "An output variable name must not be digits only; "
            + "that namespace belongs to the positional bind markers.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_NoArguments_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into());

        Assert.Equal("INTO requires at least one output parameter.", ex.Message);
    }

    [Fact]
    public void ReturningInto_VariableCountMismatch_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new OutputParameter("b", DbType.Int32))
            .Build());

        Assert.Equal(
            "INTO requires one output parameter per RETURNING expression "
            + "(2 expected, 1 provided).",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_DuplicateVariableName_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("b", DbType.Int32)));

        Assert.Equal(
            "A RETURNING INTO clause requires a distinct name for every variable; 'b' "
                + "is duplicated.",
            ex.Message);
    }

    [Fact]
    public void ReturningInto_DuplicateVariableName_LeavesTheStageRetryable()
    {
        // Into freezes the stage it completes, so a Build()-time throw left
        // only "already built" to retry against (#569).
        IReturningBuilder held = DeleteFrom(_t).Returning(_t.Code, _t.Name);
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            held.Into(new("b", DbType.Int32), new("b", DbType.Int32)));

        SqlStatement sql = held
            .Into(new("b", DbType.Int32), new("c", DbType.String, 50))
            .Build(Dbms.Oracle);

        Assert.Equal(
            "A RETURNING INTO clause requires a distinct name for every variable; 'b' "
                + "is duplicated.",
            ex.Message);
        Assert.Equal(
            "DELETE FROM test_table RETURNING code, name INTO :b, :c",
            sql.Text);
        Dictionary<string, BindValue> parameters = new();
        sql.Parameters.ForEach((name, bind) => parameters.Add(name, bind));
        Assert.Equal([":b", ":c"], parameters.Keys);
        Assert.Equal(ParameterDirection.Output, parameters[":b"].Direction);
        Assert.Equal(50, parameters[":c"].Size);
    }

    [Fact]
    public void OutputParameter_EmptyVariable_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new OutputParameter("", DbType.Int32));

        Assert.Equal("An output variable name is required.", ex.Message);
    }

    // ── output parameters ─────────────────────────────────────────────

    [Fact]
    public void ReturningInto_OnDelete_RegistersOutputParameters()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.Oracle);

        Dictionary<string, BindValue> parameters = new();
        sql.Parameters.ForEach((name, bind) => parameters.Add(name, bind));

        Assert.Equal(ParameterDirection.Output, parameters[":b"].Direction);
        Assert.Equal(DbType.Int32, parameters[":b"].DbType);
        Assert.Equal(ParameterDirection.Output, parameters[":c"].Direction);
        Assert.Equal(DbType.String, parameters[":c"].DbType);
        Assert.Equal(100, parameters[":c"].Size);
    }

    [Fact]
    public void ReturningInto_SqlServer_UsesDialectParameterMarker()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Returning(_t.Code, _t.Name)
            .Into(new("b", DbType.Int32), new("c", DbType.String, 100))
            .Build(Dbms.SqlServer);

        Assert.Equal(
            "DELETE FROM test_table RETURNING code, name INTO @b, @c",
            sql.Text);
        Assert.Contains("@b", sql.Parameters.ParameterNames);
        Assert.Contains("@c", sql.Parameters.ParameterNames);
    }

    // ── SQLite: an aliased target qualifies by table name (#595) ─────────

    [Fact]
    public void Returning_Sqlite_AliasedUpdateTarget_CorrectSql()
    {
        TestTable t = new("t");
        SqlStatement sql =
            Update(t)
            .Set(t.Name == "a")
            .Where(t.Code == 1)
            .Returning(t.Code, t.Name)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = :0 ");
        expected.Append("WHERE \"t\".code = :1 ");
        expected.Append("RETURNING test_table.code, test_table.name");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal(1, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void Returning_Sqlite_AliasedInsertTarget_CorrectSql()
    {
        TestTable t = new("t");
        SqlStatement sql =
            InsertInto(t, t.Code, t.Name)
            .Values(1, "a")
            .OnConflict(t.Code)
            .DoUpdateSet(t.Name == Excluded(t.Name))
            .Returning(t.Code)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table AS \"t\" (code, name) ");
        expected.Append("VALUES (:0, :1) ");
        expected.Append("ON CONFLICT (code) DO UPDATE SET name = excluded.name ");
        expected.Append("RETURNING test_table.code");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Returning_Sqlite_AliasedDeleteTarget_CorrectSql()
    {
        TestTable t = new("t");
        SqlStatement sql =
            DeleteFrom(t)
            .Where(t.Code == 1)
            .Returning(t.Code)
            .Build(Dbms.Sqlite);

        Assert.Equal(
            "DELETE FROM test_table AS \"t\" WHERE \"t\".code = :0 RETURNING test_table.code",
            sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Returning_Sqlite_JoinedUpdate_QualifiesOnlyTheTarget()
    {
        // A FROM relation stays alias-qualified: SQLite 3.50.4's RETURNING cannot
        // read it, and the table name would not reach it either, so it still fails.
        TestTable t = new("t");
        ArchiveTable a = new("a");
        SqlStatement sql =
            Update(t)
            .Set(t.Name == a.Name)
            .From(a)
            .Where(a.Code == t.Code)
            .Returning(t.Code, a.Code)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = \"a\".name ");
        expected.Append("FROM archive_table \"a\" ");
        expected.Append("WHERE \"a\".code = \"t\".code ");
        expected.Append("RETURNING test_table.code, \"a\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_SelfJoinedUpdate_QualifiesOnlyTheTargetInstance()
    {
        TestTable t = new("t");
        TestTable t2 = new("t2");
        SqlStatement sql =
            Update(t)
            .Set(t.Name == t2.Name)
            .From(t2)
            .Where(t2.Code == t.Code)
            .Returning(t.Code)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("UPDATE test_table AS \"t\" ");
        expected.Append("SET name = \"t2\".name ");
        expected.Append("FROM test_table \"t2\" ");
        expected.Append("WHERE \"t2\".code = \"t\".code ");
        expected.Append("RETURNING test_table.code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_SchemaQualifiedTarget_DropsTheSchema()
    {
        DbTable t = new("main.test_table", "t");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(t.Column("code"))
            .Build(Dbms.Sqlite);

        Assert.Equal(
            "DELETE FROM main.test_table AS \"t\" RETURNING test_table.code",
            sql.Text);
    }

    [Theory]
    [InlineData(Dbms.MySql)]
    [InlineData(Dbms.Oracle)]
    [InlineData(Dbms.PostgreSql)]
    public void Returning_AliasedTarget_OtherDbms_KeepsTheAlias(Dbms dbms)
    {
        TestTable t = new("t");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(t.Code)
            .Build(dbms);

        string separator = dbms == Dbms.Oracle ? " " : " AS ";
        char quote = dbms == Dbms.MySql ? '`' : '"';
        Assert.Equal(
            $"DELETE FROM test_table{separator}{quote}t{quote} RETURNING {quote}t{quote}.code",
            sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_CorrelatedSubquery_CorrectSql()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(t.Code, Select(Count(Asterisk)).From(a).Where(a.Code == t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING test_table.code, ");
        expected.Append("(SELECT COUNT(*) FROM archive_table \"a\" ");
        expected.Append("WHERE \"a\".code = test_table.code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // A scope exposing the table name would bind test_table.code to its own
    // relation, so the alias stays and SQLite 3.50.4 rejects it; one exposing the alias
    // reads the written "t".code itself, which a swap would send to the target.

    [Fact]
    public void Returning_Sqlite_SubqueryRebindingTheTableName_KeepsTheAlias()
    {
        TestTable t = new("t");
        TestTable inner = new();
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(t.Code, Select(Count(Asterisk)).From(inner).Where(inner.Code < t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING test_table.code, ");
        expected.Append("(SELECT COUNT(*) FROM test_table WHERE code < \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_SubqueryRebindingTheAlias_KeepsTheAlias()
    {
        TestTable t = new("t");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(t.Code, Select(Count(Asterisk)).From(t).Where(t.Code < 9))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING test_table.code, ");
        expected.Append("(SELECT COUNT(*) FROM test_table \"t\" WHERE \"t\".code < :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(9, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void Returning_Sqlite_SubqueryRebindingTheNameInAnotherCase_KeepsTheAlias()
    {
        TestTable t = new("t");
        ArchiveTable inner = new("Test_Table");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(Select(Count(Asterisk)).From(inner).Where(inner.Code < t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (SELECT COUNT(*) FROM archive_table \"Test_Table\" ");
        expected.Append("WHERE \"Test_Table\".code < \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_JoinRebindingTheTableName_KeepsTheAlias()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        TestTable inner = new();
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(
                Select(Count(Asterisk))
                .From(a)
                .InnerJoin(inner)
                .On(inner.Code == a.Code)
                .Where(a.Code == t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (SELECT COUNT(*) FROM archive_table \"a\" ");
        expected.Append("INNER JOIN test_table ON code = \"a\".code ");
        expected.Append("WHERE \"a\".code = \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_NestedSubqueryRebindingTheTableName_KeepsTheAliasThere()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        TestTable inner = new();
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(
                Select(Count(Asterisk))
                .From(a)
                .Where(a.Code == t.Code
                    & Exists(Select(inner.Code).From(inner).Where(inner.Code == t.Code))))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (SELECT COUNT(*) FROM archive_table \"a\" ");
        expected.Append("WHERE (\"a\".code = test_table.code) ");
        expected.Append("AND (EXISTS (SELECT code FROM test_table WHERE code = \"t\".code)))");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_CteNamedAsTheTable_KeepsTheAlias()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        TestCte c = new("test_table");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(
                With(c.As(Select(a.Code.As("cte_code")).From(a)))
                .Select(Count(Asterisk))
                .From(c)
                .Where(c.CteCode < t.Code))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (WITH \"test_table\" AS ");
        expected.Append("(SELECT \"a\".code \"cte_code\" FROM archive_table \"a\") ");
        expected.Append("SELECT COUNT(*) FROM \"test_table\" ");
        expected.Append("WHERE \"test_table\".cte_code < \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_CorrelatedCteBody_CorrectSql()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        TestCte c = new("c");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(
                With(c.As(Select(a.Code).From(a).Where(a.Code == t.Code)))
                .Select(Count(Asterisk))
                .From(c))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (WITH \"c\" AS ");
        expected.Append("(SELECT \"a\".code FROM archive_table \"a\" ");
        expected.Append("WHERE \"a\".code = test_table.code) ");
        expected.Append("SELECT COUNT(*) FROM \"c\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Returning_Sqlite_CteBodyRebindingTheTableName_KeepsTheAlias()
    {
        TestTable t = new("t");
        TestTable inner = new();
        TestCte c = new("c");
        SqlStatement sql =
            DeleteFrom(t)
            .Returning(
                With(c.As(Select(inner.Code).From(inner).Where(inner.Code < t.Code)))
                .Select(Count(Asterisk))
                .From(c))
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("RETURNING (WITH \"c\" AS ");
        expected.Append("(SELECT code FROM test_table WHERE code < \"t\".code) ");
        expected.Append("SELECT COUNT(*) FROM \"c\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
