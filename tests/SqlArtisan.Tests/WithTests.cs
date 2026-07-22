using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WithTests
{
    [Fact]
    public void Insert_WithSelect_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new();
        TestTable c = new("c");
        SqlStatement sql =
            InsertInto(
                b,
                b.Code,
                b.Name,
                b.CreatedAt)
            .With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)))
            .Select(
                    c.Code,
                    c.Name,
                    c.CreatedAt)
                .From(c)
                .InnerJoin(cte)
                .On(c.Code == cte.CteCode)
                .Where(c.Code > 0)
            .Build();

        StringBuilder expected = new();
        expected.Append("INSERT INTO test_table (code, name, created_at) ");
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\" WHERE \"a\".code = :0) ");
        expected.Append("SELECT \"c\".code, \"c\".name, \"c\".created_at ");
        expected.Append("FROM test_table \"c\" ");
        expected.Append("INNER JOIN \"cte\" ON \"c\".code = \"cte\".cte_code ");
        expected.Append("WHERE \"c\".code > :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_Select_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)))
            .Select(
                b.Code,
                b.Name,
                b.CreatedAt)
            .From(b)
            .InnerJoin(cte)
            .On(b.Code == cte.CteCode)
            .Where(b.Code > 0)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table ");
        expected.Append("\"a\" WHERE \"a\".code = :0) ");
        expected.Append("SELECT \"b\".code, \"b\".name, \"b\".created_at ");
        expected.Append("FROM test_table \"b\" ");
        expected.Append("INNER JOIN \"cte\" ");
        expected.Append("ON \"b\".code = \"cte\".cte_code ");
        expected.Append("WHERE \"b\".code > :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_SelectDistinct_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");
        ;

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)))
                    .Select(
                        Distinct,
                        b.Code)
            .From(b)
            .InnerJoin(cte)
            .On(b.Code == cte.CteCode)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\") ");
        expected.Append("SELECT DISTINCT \"b\".code ");
        expected.Append("FROM test_table \"b\" ");
        expected.Append("INNER JOIN \"cte\" ");
        expected.Append("ON \"b\".code = \"cte\".cte_code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_SelectDistinctOn_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)))
                    .Select(
                        DistinctOn(b.Code),
                        b.Code)
            .From(b)
            .InnerJoin(cte)
            .On(b.Code == cte.CteCode)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\") ");
        expected.Append("SELECT DISTINCT ON (\"b\".code) \"b\".code ");
        expected.Append("FROM test_table \"b\" ");
        expected.Append("INNER JOIN \"cte\" ");
        expected.Append("ON \"b\".code = \"cte\".cte_code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_SelectHints_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)))
            .Select(
                Hints("/*+ FULL(b) */"),
                b.Code)
            .From(b)
            .InnerJoin(cte)
            .On(b.Code == cte.CteCode)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\") ");
        expected.Append("SELECT /*+ FULL(b) */ \"b\".code ");
        expected.Append("FROM test_table \"b\" ");
        expected.Append("INNER JOIN \"cte\" ");
        expected.Append("ON \"b\".code = \"cte\".cte_code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_SelectHintsDistinct_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)))
            .Select(
                Hints("/*+ FULL(b) */"),
                Distinct,
                b.Code)
            .From(b)
            .InnerJoin(cte)
            .On(b.Code == cte.CteCode)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\") ");
        expected.Append("SELECT /*+ FULL(b) */ DISTINCT \"b\".code ");
        expected.Append("FROM test_table \"b\" ");
        expected.Append("INNER JOIN \"cte\" ");
        expected.Append("ON \"b\".code = \"cte\".cte_code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_MultiCtes_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte1 = new("cte1");

        TestTable b = new("b");
        TestCte cte2 = new("cte2");

        TestTable c = new("c");
        SqlStatement sql =
            With(
                cte1.As(
                    Select(
                        a.Code.As(cte1.CteCode),
                        a.Name.As(cte1.CteName),
                        a.CreatedAt.As(cte1.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)),
                cte2.As(
                    Select(
                        b.Code.As(cte2.CteCode),
                        b.Name.As(cte2.CteName),
                        b.CreatedAt.As(cte2.CteCreatedAt))
                    .From(b)
                    .Where(b.Code == 2)))
            .Select(c.Code, c.Name, c.CreatedAt)
            .From(c)
            .LeftJoin(cte1)
            .On(c.Code == cte1.CteCode)
            .LeftJoin(cte2)
            .On(c.Code == cte2.CteCode)
            .Where(c.Code > 0)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte1\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\" ");
        expected.Append("WHERE \"a\".code = :0), ");
        expected.Append("\"cte2\" AS ");
        expected.Append("(SELECT \"b\".code cte_code, ");
        expected.Append("\"b\".name cte_name, ");
        expected.Append("\"b\".created_at cte_created_at ");
        expected.Append("FROM test_table \"b\" WHERE \"b\".code = :1) ");
        expected.Append("SELECT \"c\".code, \"c\".name, \"c\".created_at ");
        expected.Append("FROM test_table \"c\" LEFT JOIN \"cte1\" ");
        expected.Append("ON \"c\".code = \"cte1\".cte_code ");
        expected.Append("LEFT JOIN \"cte2\" ON \"c\".code = \"cte2\".cte_code ");
        expected.Append("WHERE \"c\".code > :2"); ;

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void WithRecursive_Select_CorrectSql()
    {
        TestTable a = new("a");
        TestTable b = new("b");
        TestCte cte = new("cte");

        SqlStatement sql =
            WithRecursive(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)
                    .UnionAll
                    .Select(
                        b.Code + 1,
                        b.Name,
                        b.CreatedAt)
                    .From(cte)
                    .InnerJoin(b)
                    .On(cte.CteCode == b.Code)))
            .Select(
                cte.CteCode,
                cte.CteName,
                cte.CteCreatedAt)
            .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH RECURSIVE \"cte\"(cte_code, cte_name, cte_created_at) AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\" WHERE \"a\".code = :0 ");
        expected.Append("UNION ALL SELECT (\"b\".code + :1), ");
        expected.Append("\"b\".name, \"b\".created_at ");
        expected.Append("FROM \"cte\" INNER JOIN test_table \"b\" ");
        expected.Append("ON \"cte\".cte_code = \"b\".code) ");
        expected.Append("SELECT \"cte\".cte_code, ");
        expected.Append("\"cte\".cte_name, \"cte\".cte_created_at ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_Delete_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)
                    .Where(a.Code == 1)))
            .DeleteFrom(b)
            .Where(b.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\" ");
        expected.Append("WHERE \"a\".code = :0) ");
        expected.Append("DELETE FROM test_table AS \"b\" ");
        expected.Append("WHERE \"b\".code IN ");
        expected.Append("(SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_InsertValues_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new();
        SqlStatement sql =
            With(
                cte.As(
                    Select(a.Code.As(cte.CteCode))
                    .From(a)
                    .Where(a.Code == 1)))
            .InsertInto(b)
            .Set(
                b.Code == 1,
                b.Name == "Test",
                b.CreatedAt == Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code ");
        expected.Append("FROM test_table \"a\" ");
        expected.Append("WHERE \"a\".code = :0) ");
        expected.Append("INSERT INTO test_table (code, name, created_at) ");
        expected.Append("VALUES (:1, :2, SYSDATE)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_InsertSelect_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new();
        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)))
            .InsertInto(
                b,
                b.Code,
                b.Name,
                b.CreatedAt)
            .Select(
                cte.CteCode,
                cte.CteName,
                cte.CteCreatedAt)
            .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\" ");
        expected.Append("WHERE \"a\".code = :0) ");
        expected.Append("INSERT INTO test_table (code, name, created_at) ");
        expected.Append("SELECT \"cte\".cte_code, ");
        expected.Append("\"cte\".cte_name, ");
        expected.Append("\"cte\".cte_created_at ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_InsertIgnore_MySql_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new();
        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName))
                    .From(a)
                    .Where(a.Code == 1)))
            .InsertIgnoreInto(b, b.Code, b.Name)
            .Select(cte.CteCode, cte.CteName)
            .From(cte)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("WITH `cte` AS ");
        expected.Append("(SELECT `a`.code cte_code, ");
        expected.Append("`a`.name cte_name ");
        expected.Append("FROM test_table `a` ");
        expected.Append("WHERE `a`.code = ?0) ");
        expected.Append("INSERT IGNORE INTO test_table (code, name) ");
        expected.Append("SELECT `cte`.cte_code, ");
        expected.Append("`cte`.cte_name ");
        expected.Append("FROM `cte`");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_Update_CorrectSql()
    {
        TestTable a = new("a");
        TestCte cte = new("cte");

        TestTable b = new("b");
        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        a.Code.As(cte.CteCode),
                        a.Name.As(cte.CteName),
                        a.CreatedAt.As(cte.CteCreatedAt))
                    .From(a)
                    .Where(a.Code == 1)))
            .Update(b)
            .Set(
                b.Code == 2,
                b.Name == "Test",
                b.CreatedAt == Sysdate)
            .Where(b.Code.In(Select(cte.CteCode).From(cte)))
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code cte_code, ");
        expected.Append("\"a\".name cte_name, ");
        expected.Append("\"a\".created_at cte_created_at ");
        expected.Append("FROM test_table \"a\" ");
        expected.Append("WHERE \"a\".code = :0) ");
        expected.Append("UPDATE test_table AS \"b\" ");
        expected.Append("SET code = :1, ");
        expected.Append("name = :2, ");
        expected.Append("created_at = SYSDATE ");
        expected.Append("WHERE \"b\".code IN ");
        expected.Append("(SELECT \"cte\".cte_code FROM \"cte\")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void With_NoCtes_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => With());

        Assert.Equal(
            "WITH requires at least one common table expression.",
            ex.Message);
    }

    [Fact]
    public void WithRecursive_NoCtes_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => WithRecursive());

        Assert.Equal(
            "WITH requires at least one common table expression.",
            ex.Message);
    }

    [Fact]
    public void WithRecursive_DbColumnSelectItems_DerivesColumnListFromColumnNames()
    {
        TestTable a = new("a");
        Cte c = new("c");

        SqlStatement sql =
            WithRecursive(
                c.As(
                    Select(a.Code, a.Name).From(a).Where(a.Code == 1)
                    .UnionAll
                    .Select(a.Code, a.Name).From(a).InnerJoin(c).On(c.Column("code") == a.Code)))
            .Select(c.Column("code"))
            .From(c)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH RECURSIVE \"c\"(code, name) AS ");
        expected.Append("(SELECT \"a\".code, \"a\".name FROM test_table \"a\" WHERE \"a\".code = :0 ");
        expected.Append("UNION ALL SELECT \"a\".code, \"a\".name FROM test_table \"a\" ");
        expected.Append("INNER JOIN \"c\" ON \"c\".code = \"a\".code) ");
        expected.Append("SELECT \"c\".code FROM \"c\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void WithRecursive_MultiCtes_EachCteGetsItsOwnColumnList()
    {
        TestTable a = new("a");
        Cte c1 = new("c1");
        Cte c2 = new("c2");

        SqlStatement sql =
            WithRecursive(
                c1.As(
                    Select(a.Code).From(a).Where(a.Code == 1)
                    .UnionAll
                    .Select(a.Code).From(a).InnerJoin(c1).On(c1.Column("code") == a.Code)),
                c2.As(
                    Select(a.Name.As("n")).From(a)))
            .Select(c1.Column("code"), c2.Column("n"))
            .From(c1, c2)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH RECURSIVE \"c1\"(code) AS ");
        expected.Append("(SELECT \"a\".code FROM test_table \"a\" WHERE \"a\".code = :0 ");
        expected.Append("UNION ALL SELECT \"a\".code FROM test_table \"a\" ");
        expected.Append("INNER JOIN \"c1\" ON \"c1\".code = \"a\".code), ");
        expected.Append("\"c2\"(n) AS ");
        expected.Append("(SELECT \"a\".name \"n\" FROM test_table \"a\") ");
        expected.Append("SELECT \"c1\".code, \"c2\".n FROM \"c1\", \"c2\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void WithRecursive_UnnamedSelectItem_ThrowsArgumentException()
    {
        TestTable a = new("a");
        Cte c = new("c");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            WithRecursive(
                c.As(
                    Select(a.Code + 1).From(a)
                    .UnionAll
                    .Select(a.Code).From(c))));

        Assert.Equal(
            "WITH RECURSIVE requires a name for every column of the CTE's first query block; "
                + "alias the expression with .As(...).",
            ex.Message);
    }
}
