using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>The <c>Build()</c> guards (<c>DmlTargetGuard</c>, <c>InsertBuilder.Validate</c>) a
/// DML model level's verdict reads, each with the smallest statements that reach it.</summary>
internal static class BuildGuards
{
    // One handle per alias: the re-list guards compare the target with a FROM table by
    // instance, so a probe names each table through the same handle.
    private static readonly RandomUsersTable s_u = new("u");
    private static readonly RandomUsersTable s_bare = new("");
    private static readonly RandomUsersTable s_u2 = new("u2");
    private static readonly RandomDeptsTable s_d = new("d");
    private static readonly Cte s_ids = new("ids");

    public static readonly GuardKey InsertTargetAliasedOnMySql = new(
        "DmlTargetGuard.ThrowIfInsertTargetAliasedOnMySql",
        [Dbms.MySql],
        "MySQL does not support aliasing the target of an INSERT statement; use an unaliased "
            + "target table.",
        [() => InsertInto(Users("u"), Users("u").Id).Values(500)]);

    public static readonly GuardKey InsertTargetAliasedOnSqlServer = new(
        "DmlTargetGuard.ThrowIfAliasedOnSqlServer",
        [Dbms.SqlServer],
        "SQL Server does not support aliasing the target of an INSERT statement; use an "
            + "unaliased target table.",
        [() => InsertInto(Users("u"), Users("u").Id).Values(500)]);

    public static readonly GuardKey LeadingWithOnOracle = new(
        "DmlTargetGuard.ThrowIfLeadingWithUnsupported (Oracle)",
        [Dbms.Oracle],
        "Oracle has no leading WITH on INSERT, UPDATE, or DELETE; for an INSERT, put the CTE "
            + "inside the feeding SELECT (InsertInto(...).With(...).Select(...)), otherwise "
            + "inline the subquery.",
        [
            () => WithIds().InsertInto(Users(""), Users("").Id).Select(Ids().Column("k") + 100)
                .From(Ids()),
            () => WithIds().Update(Users("")).Set(Users("").Age == 1)
                .Where(Users("").Id.In(Select(Ids().Column("k")).From(Ids()))),
            () => WithIds().DeleteFrom(Users(""))
                .Where(Users("").Id.In(Select(Ids().Column("k")).From(Ids()))),
        ]);

    public static readonly GuardKey LeadingWithOnMySqlInsert = new(
        "DmlTargetGuard.ThrowIfLeadingWithUnsupported (MySQL INSERT)",
        [Dbms.MySql],
        "MySQL has no leading WITH on INSERT; put the CTE inside the feeding SELECT "
            + "(InsertInto(...).With(...).Select(...)), otherwise inline the subquery.",
        [
            () => WithIds().InsertInto(Users(""), Users("").Id).Select(Ids().Column("k") + 100)
                .From(Ids()),
        ]);

    public static readonly GuardKey DirectJoinUpdateOnSqlServer = new(
        "DmlTargetGuard.ThrowIfSqlServerJoinedTargetNotRepeated (direct join)",
        [Dbms.SqlServer],
        "A joined UPDATE on SQL Server must join through From(target, ...), re-listing the "
            + "target table.",
        [
            () => Update(Users("u")).InnerJoin(s_d).On(Users("u").DeptId == s_d.Id)
                .Set(Users("u").Name == s_d.Name),
        ]);

    public static readonly GuardKey UpdateFromNotRelistedOnSqlServer = new(
        "DmlTargetGuard.ThrowIfSqlServerJoinedTargetNotRepeated (FROM)",
        [Dbms.SqlServer],
        "A joined UPDATE on SQL Server must re-list the target table in the FROM clause.",
        [
            () => Update(Users("u")).Set(Users("u").Name == s_d.Name).From(s_d)
                .Where(Users("u").DeptId == s_d.Id),
        ]);

    public static readonly GuardKey UpdateRelistedOffSqlServer = new(
        "DmlTargetGuard.ThrowIfUpdateTargetRepeatedOffSqlServer",
        [Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite],
        "Only SQL Server supports a joined UPDATE that re-lists the target table in the FROM "
            + "clause.",
        [
            () => Update(Users("u")).Set(Users("u").Name == s_d.Name).From(Users("u"))
                .InnerJoin(s_d).On(Users("u").DeptId == s_d.Id),
        ]);

    public static readonly GuardKey DeleteUsingOnSqlServer = new(
        "DmlTargetGuard.ThrowIfSqlServerDeleteUsing",
        [Dbms.SqlServer],
        "SQL Server has no DELETE ... USING form; join through From(...), re-listing the "
            + "target table.",
        [() => DeleteFrom(Users("u")).Using(s_d).Where(Users("u").DeptId == s_d.Id)]);

    public static readonly GuardKey TargetlessDoUpdateOnPostgreSql = new(
        "InsertBuilder.Validate (targetless DO UPDATE on PostgreSQL)",
        [Dbms.PostgreSql],
        "PostgreSQL requires a conflict target for ON CONFLICT DO UPDATE; name the column(s) in "
            + "OnConflict(...).",
        [
            () => InsertInto(Users(""), Users("").Id).Values(500).OnConflict()
                .DoUpdateSet(Users("").Age == 1),
        ]);

    public static readonly GuardKey LeadingWithOnOracleMerge = new(
        "DmlTargetGuard.ThrowIfLeadingWithUnsupportedOnMerge",
        [Dbms.Oracle],
        "Oracle has no leading WITH on MERGE; put the CTE inside the subquery the Using(...) "
            + "source names.",
        [
            () => WithIds().MergeInto(s_u).Using(Ids()).On(s_u.Id == Ids().Column("k"))
                .WhenMatched().ThenUpdateSet(s_u.Age == 1),
        ]);

    public static readonly IReadOnlyList<GuardKey> All =
    [
        InsertTargetAliasedOnMySql,
        InsertTargetAliasedOnSqlServer,
        LeadingWithOnOracle,
        LeadingWithOnMySqlInsert,
        DirectJoinUpdateOnSqlServer,
        UpdateFromNotRelistedOnSqlServer,
        UpdateRelistedOffSqlServer,
        DeleteUsingOnSqlServer,
        TargetlessDoUpdateOnPostgreSql,
        LeadingWithOnOracleMerge,
    ];

    private static RandomUsersTable Users(string alias) => alias switch
    {
        "u" => s_u,
        "u2" => s_u2,
        _ => s_bare,
    };

    private static Cte Ids() => s_ids;

    private static IWithBuilderWith WithIds() =>
        With(Ids().As(Select(Users("u2").Id.As("k")).From(Users("u2"))));
}
