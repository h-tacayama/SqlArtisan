using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The #244 boundary rule: every type a user must NAME in a declaration
// position lives in the root namespace. Each test names one such type —
// deliberately with no `using SqlArtisan.Internal;` in this file.
public class PublicSurfaceNamingTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    [Fact]
    public void ISubquery_ReusableSubqueryHelper_CorrectSql()
    {
        static ISubquery PositiveCodes(TestTable s) =>
            Select(s.Code).From(s).Where(s.Code > 0);

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code.In(PositiveCodes(_s)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".name ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE \"t\".code IN ");
        expected.Append("(SELECT \"s\".code FROM test_table \"s\" WHERE \"s\".code > :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void TableReference_DynamicFromList_CorrectSql()
    {
        List<TableReference> tables = [_t, _s];

        SqlStatement sql =
            Select(_t.Code)
            .From([.. tables])
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\", test_table \"s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SortOrder_SortKeyHelper_CorrectSql()
    {
        static SortOrder SortKey(TestTable t, bool descending) =>
            descending ? t.Code.Desc : t.Code.Asc;

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(SortKey(_t, descending: true))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("ORDER BY \"t\".code DESC");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ExpressionAlias_AliasedItemHelper_CorrectSql()
    {
        static ExpressionAlias Doubled(TestTable t) => (t.Code * 2).As("doubled");

        SqlStatement sql =
            Select(Doubled(_t))
            .From(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT (\"t\".code * :0) \"doubled\" ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // A schema-object handle shared as a field — the type name is required
    // in the field declaration.
    private static readonly DbSequence CodeSeq = Sequence("code_seq");

    // A hint block shared across statements the same way.
    private static readonly SqlHints AnyHint = Hints("/*+ ANY HINT */");

    [Fact]
    public void DbSequence_SharedHandleField_CorrectSql()
    {
        SqlStatement sql = Select(CodeSeq.Nextval).Build();

        StringBuilder expected = new();
        expected.Append("SELECT code_seq.NEXTVAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SqlHints_SharedConstantField_CorrectSql()
    {
        SqlStatement sql = Select(AnyHint, _t.Code).From(_t).Build();

        StringBuilder expected = new();
        expected.Append("SELECT /*+ ANY HINT */ \"t\".code ");
        expected.Append("FROM test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LockBehaviorBase_RuntimeSelection_CorrectSql()
    {
        static LockBehaviorBase Lock(int waitSeconds) =>
            waitSeconds > 0 ? Wait(waitSeconds) : SkipLocked;

        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .ForUpdate(Lock(0))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("FOR UPDATE SKIP LOCKED");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void CommonTableExpression_DynamicWithList_CorrectSql()
    {
        Cte c1 = new("c1");
        Cte c2 = new("c2");
        List<CommonTableExpression> ctes =
        [
            c1.As(Select(_s.Code.As(c1.Column("code"))).From(_s)),
            c2.As(Select(_s.Name.As(c2.Column("name"))).From(_s)),
        ];

        SqlStatement sql =
            With([.. ctes])
            .Select(c1.Column("code"))
            .From(c1)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"c1\" AS ");
        expected.Append("(SELECT \"s\".code code FROM test_table \"s\"), ");
        expected.Append("\"c2\" AS ");
        expected.Append("(SELECT \"s\".name name FROM test_table \"s\") ");
        expected.Append("SELECT \"c1\".code ");
        expected.Append("FROM \"c1\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
