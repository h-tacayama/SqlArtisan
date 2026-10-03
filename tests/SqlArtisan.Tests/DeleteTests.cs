using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class DeleteTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void DeleteFrom_SimpleTable_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_WithWhereClause_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_Oracle_WithWhereClause_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933), so the alias follows
        // the table name with only a space.
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SqlServer_AliasedTarget_ThrowsArgumentException()
    {
        // T-SQL cannot alias the DELETE target directly, so the aliased form has
        // no valid spelling on SQL Server — the guard throws at Build (ADR 0011).
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t).Where(_t.Code == 1).Build(Dbms.SqlServer));

        Assert.Equal(
            "SQL Server does not support aliasing the target of an INSERT, UPDATE, or DELETE "
                + "statement; use an unaliased target table — a correlated UPDATE or DELETE joins "
                + "through From(...) instead.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_UnaliasedTarget_CorrectSql()
    {
        // The unaliased target builds normally on SQL Server — only the alias is
        // rejected. Columns render unqualified; parameters use the @ marker.
        TestTable t = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Where(t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE ");
        expected.Append("code = @0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_CorrelatedSubqueryUnaliasedTarget_ThrowsArgumentException()
    {
        // The bare outer column would resolve to the inner table — a silent
        // tautology deleting every row — so the guard throws at Build (#253).
        TestTable t = new();
        TestTable r = new("r");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(Select(r.Code).From(r).Where(r.Code == t.Code)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CorrelatedSubqueryAliasedTarget_CorrectSql()
    {
        TestTable r = new("r");

        SqlStatement sql =
            DeleteFrom(_t)
            .Where(Exists(Select(r.Code).From(r).Where(r.Code == _t.Code)))
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("WHERE ");
        expected.Append("EXISTS ");
        expected.Append("(SELECT \"r\".code ");
        expected.Append("FROM test_table \"r\" ");
        expected.Append("WHERE \"r\".code = \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SameTableInstanceInSubquery_ThrowsArgumentException()
    {
        // Accepted false positive: one C# instance standing for two SQL scopes is
        // ambiguous authorship — use a second instance for the inner scope, or alias the target.
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(t.Code.In(Select(t.Code).From(t)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CteBodyReferencingTarget_CorrectSql()
    {
        // The body lists the target as its own relation, so its reference reads
        // that relation rather than correlating, and the guard leaves it alone.
        TestTable t = new();
        TestCte cte = new("cte");

        SqlStatement sql =
            With(cte.As(Select(t.Code.As(cte.CteCode)).From(t)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT code cte_code FROM test_table) ");
        expected.Append("DELETE FROM test_table ");
        expected.Append("WHERE code IN ");
        expected.Append("(SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // A CTE body that never lists the target can only reach it by correlation,
    // where a bare target column can bind a same-named column of the body's own
    // relation (#607).
    [Fact]
    public void DeleteFrom_CteBodyInSubqueryCorrelatingUnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(
                With(cte.As(Select(r.Code.As(cte.CteCode)).From(r).Where(r.Code == t.Code)))
                .Select(cte.CteCode)
                .From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // A top-level body is no shelter: the bare code binds "r".code here.
    [Fact]
    public void DeleteFrom_TopLevelCteBodyCorrelatingUnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(Select(r.Code.As(cte.CteCode)).From(r).Where(r.Code == t.Code)))
            .DeleteFrom(t)
            .Where(Exists(Select(cte.CteCode).From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CteBodyNestedSubqueryCorrelatingUnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestTable x = new("x");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(
                Select(r.Code.As(cte.CteCode))
                .From(r)
                .Where(Exists(Select(x.Code).From(x).Where(x.Code == t.Code)))))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // A listing in a descendant block does not scope the correlating block: the
    // target is only the relation of the block that lists it.
    [Fact]
    public void DeleteFrom_CteBodyDescendantSubqueryListingTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(
                With(cte.As(
                    Select(r.Code.As(cte.CteCode))
                    .From(r)
                    .Where((r.Code == t.Code) & r.Code.In(Select(t.Code).From(t)))))
                .Select(cte.CteCode)
                .From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // Nor does a sibling block's listing: one subquery reads the target, another
    // correlates with it.
    [Fact]
    public void DeleteFrom_CteBodySiblingSubqueryListingTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestTable x = new("x");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(
                With(cte.As(
                    Select(r.Code.As(cte.CteCode))
                    .From(r)
                    .Where(Exists(Select(t.Code).From(t))
                        & Exists(Select(x.Code).From(x).Where(x.Code == t.Code)))))
                .Select(cte.CteCode)
                .From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // An enclosing block's listing does not scope a nested body either, wherever
    // the nested body sits: a relation in between could shadow the column.
    [Theory]
    [InlineData("select")]
    [InlineData("where")]
    public void DeleteFrom_NestedCteBodyUnderListingBody_ThrowsArgumentException(string clause)
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte outer = new("outer_cte");
        TestCte inner = new("inner_cte");
        ISubquery nested =
            With(inner.As(Select(r.Code.As(inner.CteCode)).From(r).Where(r.Code == t.Code)))
            .Select(inner.CteCode)
            .From(inner);
        ISubquery body = clause == "select"
            ? Select(t.Code.As(outer.CteCode), nested.As("n")).From(t)
            : Select(t.Code.As(outer.CteCode)).From(t).Where(Exists(nested));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(outer.As(body))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(outer.CteCode).From(outer)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // Each set-operator branch is its own block: a branch listing the target does
    // not scope another that correlates with it, in either order (#611).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeleteFrom_CteBodyBranchCorrelatingUnaliasedTarget_ThrowsArgumentException(
        bool listingFirst)
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");
        ISubquery body = listingFirst
            ? Select(t.Code.As(cte.CteCode)).From(t)
                .Union.Select(r.Code).From(r).Where(r.Code == t.Code)
            : Select(r.Code.As(cte.CteCode)).From(r).Where(r.Code == t.Code)
                .Union.Select(t.Code).From(t);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(body))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_NestedCteBodyBranchCorrelatingUnaliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(
                With(cte.As(
                    Select(t.Code.As(cte.CteCode)).From(t)
                    .Union.Select(r.Code).From(r).Where(r.Code == t.Code)))
                .Select(cte.CteCode)
                .From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CteBodySetOperatorBranchesEachListingTarget_CorrectSql()
    {
        TestTable t = new();
        TestCte cte = new("cte");

        SqlStatement sql =
            With(cte.As(
                Select(t.Code.As(cte.CteCode)).From(t)
                .Union.Select(t.Name).From(t)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT code cte_code FROM test_table ");
        expected.Append("UNION SELECT name FROM test_table) ");
        expected.Append("DELETE FROM test_table ");
        expected.Append("WHERE code IN ");
        expected.Append("(SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // A branch listing the target lets the compound's ORDER BY name the target's column.
    [Fact]
    public void DeleteFrom_CteBodyCompoundOrderByTargetColumn_CorrectSql()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");
        DbColumn cteCode = new(cte, "code");

        SqlStatement sql =
            With(cte.As(
                Select(t.Code).From(t)
                .Union.Select(r.Code).From(r)
                .OrderBy(t.Code)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT code FROM test_table ");
        expected.Append("UNION SELECT \"r\".code FROM test_table \"r\" ");
        expected.Append("ORDER BY code) ");
        expected.Append("DELETE FROM test_table ");
        expected.Append("WHERE code IN ");
        expected.Append("(SELECT \"cte\".code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // With no branch listing the target, the ORDER BY's target column is rejected as
    // in any block that does not list it.
    [Fact]
    public void DeleteFrom_CteBodyOrderByTargetColumnNoListing_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestTable x = new("x");
        TestCte cte = new("cte");
        DbColumn cteCode = new(cte, "code");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(
                Select(r.Code).From(r)
                .Union.Select(x.Code).From(x)
                .OrderBy(t.Code)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cteCode).From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    // The last branch is checked before the compound's ORDER BY takes over.
    [Fact]
    public void DeleteFrom_CteBodyLastBranchCorrelatingBeforeOrderBy_ThrowsArgumentException()
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");
        DbColumn cteCode = new(cte, "code");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            With(cte.As(
                Select(t.Code).From(t)
                .Union.Select(r.Code).From(r).Where(r.Code == t.Code)
                .OrderBy(t.Code)))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cteCode).From(cte)))
            .Build());

        Assert.Equal(
            "The target of a correlated UPDATE, DELETE, or MERGE must be aliased.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_CteBodyInSubqueryCorrelatingAliasedTarget_CorrectSql()
    {
        TestTable t = new("t");
        TestTable r = new("r");
        TestCte cte = new("cte");

        SqlStatement sql =
            DeleteFrom(t)
            .Where(Exists(
                With(cte.As(Select(r.Code.As(cte.CteCode)).From(r).Where(r.Code == t.Code)))
                .Select(cte.CteCode)
                .From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("WHERE EXISTS (WITH \"cte\" AS ");
        expected.Append("(SELECT \"r\".code cte_code FROM test_table \"r\" ");
        expected.Append("WHERE \"r\".code = \"t\".code) ");
        expected.Append("SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Delete_WhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(_t)
            .Where(ConditionIf(false, _t.Code > 0))
            .Build());

        Assert.Equal(
            "The WHERE clause requires a condition; omit it for an unfiltered statement.",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_PostgreSql_Using_CorrectSql()
    {
        // PostgreSQL DELETE ... USING: the target keeps its `DELETE FROM target`
        // lead; the join predicate lives in WHERE.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            DeleteFrom(t)
            .Using(s)
            .Where(t.Code == s.Code)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_PostgreSql_UsingWithReturning_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            DeleteFrom(t)
            .Using(s)
            .Where(t.Code == s.Code)
            .Returning(t.Code)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table AS \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("WHERE \"t\".code = \"s\".code ");
        expected.Append("RETURNING \"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SqlServer_FromJoin_CorrectSql()
    {
        // SQL Server leads with the FROM-defined alias (`DELETE "t"`) and
        // re-lists the target in FROM.
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            DeleteFrom(t)
            .From(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("DELETE \"t\" ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("INNER JOIN test_table \"s\" ");
        expected.Append("ON \"t\".code = \"s\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SqlServer_FromJoinOnAllConditionsExcluded_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .From(t)
            .InnerJoin(s).On(ConditionIf(false, t.Code == s.Code))
            .Build(Dbms.SqlServer));

        Assert.Equal("A JOIN's ON clause requires a condition.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_MySql_FromJoin_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            DeleteFrom(t)
            .From(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("DELETE `t` ");
        expected.Append("FROM test_table `t` ");
        expected.Append("INNER JOIN test_table `s` ");
        expected.Append("ON `t`.code = `s`.code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_FromNotRepeatingTarget_ThrowsAtFrom()
    {
        // The SQL Server / MySQL joined DELETE must re-list its target in FROM;
        // omitting it would emit a target-less DELETE alias.
        TestTable t = new("t");
        TestTable s = new("s");
        IDeleteBuilderDeleteOutput held = DeleteFrom(t);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => held.From(s));

        Assert.Equal(
            "A joined DELETE ... FROM must re-list the target table in the FROM clause.",
            ex.Message);
        Assert.Equal(
            "DELETE \"t\" FROM test_table \"t\", test_table \"s\"",
            held.From(t, s).Build(Dbms.SqlServer).Text);
    }

    [Fact]
    public void DeleteFrom_UsingNoTables_ThrowsArgumentException()
    {
        TestTable t = new("t");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t).Using());

        Assert.Equal("USING requires at least one table.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_FromNoTables_ThrowsArgumentException()
    {
        TestTable t = new("t");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t).From());

        Assert.Equal("FROM requires at least one table.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_Output_CorrectSql()
    {
        TestTable t = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Output(Deleted(t.Code))
            .Where(t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table ");
        expected.Append("OUTPUT DELETED.code ");
        expected.Append("WHERE code = @0");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>("@0"));
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputInto_CorrectSql()
    {
        // The single-statement archive-then-delete form (SQL Server): OUTPUT ...
        // INTO precedes WHERE.
        TestTable t = new();
        ArchiveTable a = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Output(Deleted(t.Code), Deleted(t.Name))
            .Into(a, a.Code, a.Name)
            .Where(t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("DELETE FROM test_table ");
        expected.Append("OUTPUT DELETED.code, DELETED.name ");
        expected.Append("INTO archive_table (code, name) ");
        expected.Append("WHERE code = @0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputAlias_CorrectSql()
    {
        // SQL Server permits an OUTPUT column alias, so aliases are not rejected.
        TestTable t = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Output(Deleted(t.Code).As("old_code"))
            .Where(t.Code == 1)
            .Build(Dbms.SqlServer);

        Assert.Equal(
            "DELETE FROM test_table OUTPUT DELETED.code \"old_code\" WHERE code = @0",
            sql.Text);
    }

    [Fact]
    public void DeleteFrom_OutputNoExpressions_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t).Output());

        Assert.Equal("OUTPUT requires at least one expression.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_OutputNullExpression_ThrowsArgumentNullException()
    {
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            DeleteFrom(t).Output(Deleted(t.Code), null!));

        Assert.Equal(
            "A C# null is not SQL NULL; pass Sql.Null instead. (Parameter 'outputItem')",
            ex.Message);
    }

    [Fact]
    public void DeleteFrom_OutputUnresolvableItem_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t).Output(new object()));

        Assert.Equal("Invalid type for OutputItem: System.Object", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputIntoAliasedTarget_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new("a");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Output(Deleted(t.Code))
            .Into(a, a.Code));

        Assert.Equal(
            "The destination table of OUTPUT ... INTO must not be aliased.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputAndReturning_ThrowsArgumentException()
    {
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Output(Deleted(t.Code))
            .Returning(t.Code)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with RETURNING; use one or the other.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputIntoAndReturning_ThrowsArgumentException()
    {
        TestTable t = new();
        ArchiveTable a = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Output(Deleted(t.Code))
            .Into(a, a.Code)
            .Returning(t.Code)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with RETURNING; use one or the other.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_OutputAndUsing_ThrowsArgumentException()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Output(Deleted(t.Code))
            .Using(s)
            .Where(t.Code == s.Code)
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "OUTPUT cannot be combined with USING; use one or the other.", ex.Message);
    }

    [Fact]
    public void DeleteFrom_SqlServer_Using_ThrowsArgumentException()
    {
        // The re-list remedy the joined-target guard names is unreachable from
        // a Using(...) chain, so T-SQL's missing USING form is named first.
        TestTable t = new("t");
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
                .Using(s)
                .Where(t.Code == s.Code)
                .Build(Dbms.SqlServer));

        Assert.Equal(
            "SQL Server has no DELETE ... USING form; join through From(...), "
                + "re-listing the target table.",
            ex.Message);
    }

    // Each nested block lists the target itself, so its reference reads that
    // relation and builds (release audit pass 8; checked per block since #607).
    [Theory]
    [InlineData("in")]
    [InlineData("exists")]
    [InlineData("scalar")]
    [InlineData("derived")]
    public void DeleteFrom_CteBodyNestedSubqueryReferencingTarget_CorrectSql(string nesting)
    {
        TestTable t = new();
        TestTable r = new("r");
        TestCte cte = new("cte");

        ISubquery body = nesting switch
        {
            "in" => Select(r.Code.As(cte.CteCode)).From(r).Where(r.Code.In(Select(t.Code).From(t))),
            "exists" => Select(r.Code.As(cte.CteCode)).From(r)
                .Where(Exists(Select(t.Code).From(t))),
            "scalar" => Select(Select(Max(t.Code)).From(t).As("cte_code")).From(r),
            _ => Select(r.Code.As(cte.CteCode)).From(Select(t.Code).From(t).AsTable("d"), r),
        };

        SqlStatement sql =
            With(cte.As(body))
            .DeleteFrom(t)
            .Where(t.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        Assert.StartsWith("WITH \"cte\" AS (", sql.Text);
        Assert.EndsWith(
            "DELETE FROM test_table WHERE code IN (SELECT \"cte\".cte_code FROM \"cte\")",
            sql.Text);
    }

    // No engine that spells DELETE ... FROM has RETURNING, so the stage withholds it.
    [Theory]
    [InlineData(typeof(IDeleteBuilderFrom))]
    [InlineData(typeof(IDeleteBuilderFromWhere))]
    public void JoinedDeleteStage_OffersNoReturning(Type stage)
    {
        Assert.False(typeof(IReturning).IsAssignableFrom(stage));
    }

    [Fact]
    public void DeleteFrom_HeldStageJoinedThenReturning_ThrowsAtBuild()
    {
        TestTable t = new("t");
        ArchiveTable a = new("a");
        IDeleteBuilderDeleteOutput held = DeleteFrom(t);
        held.From(t).InnerJoin(a).On(a.Code == t.Code);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            held.Returning(t.Code).Build(Dbms.PostgreSql));

        Assert.Equal(
            "RETURNING cannot be combined with a joined DELETE ... FROM; "
                + "join through Using(...) instead.",
            ex.Message);
    }
}
