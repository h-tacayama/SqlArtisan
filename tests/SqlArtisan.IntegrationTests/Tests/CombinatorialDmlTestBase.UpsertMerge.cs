using SqlArtisan.IntegrationTests.Randomized;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// The upsert and MERGE readings Randomized/UpsertMergeGenerators.cs never builds, each pinned on
// the lane whose engine reads it. Seed row 1 has age 30 and code 101, row 2 code 102, and the
// 28 seed ages sum to 508.
public sealed partial class SqliteCombinatorialDmlTests
{
    // SQLite 3.50.4 applies a batch row by row: the second row updates the first.
    [Fact]
    public void Upsert_Sqlite_KeyRepeatedInBatch_AppliesRowsInOrder()
    {
        RandomUsersTable t = new("");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Age).Values(600, 1).Values(600, 2).OnConflict(t.Id)"
                    + ".DoUpdateSet(t.Age == Excluded(t.Age))",
                () => InsertInto(t, t.Id, t.Age)
                    .Values(600, 1)
                    .Values(600, 2)
                    .OnConflict(t.Id)
                    .DoUpdateSet(t.Age == Excluded(t.Age))),
            UpsertMergeTwins.ReadAges(600));

        Assert.Null(upsert.Error);
        Assert.Equal(["600|2"], row);
    }

    [Fact]
    public void Upsert_Sqlite_ConflictOnOtherUniqueKey_IsRejectedByTheEngine()
    {
        Assert.Contains("UNIQUE constraint failed", Rejection(UpsertMergeTwins.OtherKeyConflict()));
    }

    // The starting rows give row 1 the seed sum, 508; SQLite 3.50.4 counts the row inserted
    // before it, so a batch's subqueries read rt_depts there.
    [Fact]
    public void Upsert_Sqlite_BatchSubqueryOverTarget_ReadsEarlierRows()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Age).Values(600, 100).Values(1, 0).OnConflict(t.Id)"
                    + ".DoUpdateSet(t.Age == Select(Sum(u2.Age)).From(u2))",
                () => InsertInto(t, t.Id, t.Age)
                    .Values(600, 100)
                    .Values(1, 0)
                    .OnConflict(t.Id)
                    .DoUpdateSet(t.Age == Select(Sum(u2.Age)).From(u2))),
            UpsertMergeTwins.ReadAges(1));

        Assert.Null(upsert.Error);
        Assert.Equal(["1|608"], row);
    }

    [Fact]
    public void UpdateSet_Sqlite_KeyShiftedOntoNextRow_IsRejectedByTheEngine()
    {
        Assert.Contains("UNIQUE constraint failed", Rejection(UpsertMergeTwins.ShiftedCodes()));
    }
}

public sealed partial class PostgreSqlCombinatorialDmlTests
{
    [Fact]
    public void Upsert_PostgreSql_KeyRepeatedInBatch_IsRejectedByTheEngine()
    {
        RandomUsersTable t = new("");
        string? error = Rejection(UpsertMergeTwins.Statement(
            "InsertInto(t, t.Id, t.Age).Values(600, 1).Values(600, 2).OnConflict(t.Id)"
                + ".DoUpdateSet(t.Age == Excluded(t.Age))",
            () => InsertInto(t, t.Id, t.Age)
                .Values(600, 1)
                .Values(600, 2)
                .OnConflict(t.Id)
                .DoUpdateSet(t.Age == Excluded(t.Age))));

        Assert.Contains("cannot affect row a second time", error);
    }

    // PostgreSQL 16.15 reads the starting rows, so row 1 takes the seed sum, 508, and its
    // batches' subqueries may read the target.
    [Fact]
    public void Upsert_PostgreSql_BatchSubqueryOverTarget_ReadsStartingRows()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Age).Values(600, 100).Values(1, 0).OnConflict(t.Id)"
                    + ".DoUpdateSet(t.Age == Select(Sum(u2.Age)).From(u2))",
                () => InsertInto(t, t.Id, t.Age)
                    .Values(600, 100)
                    .Values(1, 0)
                    .OnConflict(t.Id)
                    .DoUpdateSet(t.Age == Select(Sum(u2.Age)).From(u2))),
            UpsertMergeTwins.ReadAges(1));

        Assert.Null(upsert.Error);
        Assert.Equal(["1|508"], row);
    }

    [Fact]
    public void Upsert_PostgreSql_ConflictOnOtherUniqueKey_IsRejectedByTheEngine()
    {
        Assert.Contains("23505", Rejection(UpsertMergeTwins.OtherKeyConflict()));
    }

    // PostgreSQL 16.15 checks a unique key row by row, so a shift onto the next row's value
    // fails where the statement's end state is unique.
    [Fact]
    public void UpdateSet_PostgreSql_KeyShiftedOntoNextRow_IsRejectedByTheEngine()
    {
        Assert.Contains("23505", Rejection(UpsertMergeTwins.ShiftedCodes()));
    }

    [Fact]
    public void Merge_PostgreSql_TargetMatchedTwice_IsRejectedByTheEngine()
    {
        Assert.Contains("21000", Rejection(UpsertMergeTwins.TargetMatchedTwice()));
    }

    // PostgreSQL 16.15 types a VALUES column NULL in every row as text.
    [Fact]
    public void MergeValues_PostgreSql_AllNullColumn_IsRejectedByTheEngine()
    {
        RandomUsersTable u = new("u");
        ValuesDerivedTable s = Values("s", ["id", "age"], [[1, Null]]);
        string? error = Rejection(UpsertMergeTwins.Statement(
            "MergeInto(u).Using(Values(\"s\", [\"id\", \"age\"], [[1, Null]]))"
                + ".On(u.Id == s.Column(\"id\")).WhenMatched()"
                + ".ThenUpdateSet(u.Age == s.Column(\"age\"))",
            () => MergeInto(u)
                .Using(s)
                .On(u.Id == s.Column("id"))
                .WhenMatched()
                .ThenUpdateSet(u.Age == s.Column("age"))));

        Assert.Contains("is of type integer but expression is of type text", error);
    }

    // PostgreSQL 16.15 resolves UNION ALL from the left: NULL beside NULL is text, which the
    // integer branch after it cannot join; NULL beside the integer first is integer.
    [Fact]
    public void UnionAll_PostgreSql_NullPairThenInteger_IsRejectedByTheEngine()
    {
        RandomUsersTable t = new("");
        RandomDeptsTable d = new("d");
        ISqlBuilder Feed(object first, object second) => InsertInto(t, t.Id, t.Age)
            .Select(600, first).From(d).Where(d.Id == 1)
            .UnionAll.Select(601, second).From(d).Where(d.Id == 1)
            .UnionAll.Select(602, 5).From(d).Where(d.Id == 1);
        string? rejected = Rejection(UpsertMergeTwins.Statement(
            "InsertInto(t, t.Id, t.Age).Select(600, Null).From(d).Where(d.Id == 1).UnionAll"
                + ".Select(601, Null)...UnionAll.Select(602, 5)...",
            () => Feed(Null, Null)));
        string? accepted = Rejection(UpsertMergeTwins.Statement(
            "InsertInto(t, t.Id, t.Age).Select(600, Null)...UnionAll.Select(601, 5)..."
                + "UnionAll.Select(602, 5)...",
            () => Feed(Null, 5)));

        Assert.Contains("UNION types text and integer cannot be matched", rejected);
        Assert.Null(accepted);
    }
}

public sealed partial class MySqlCombinatorialDmlTests
{
    // MySQL 8.0.46 applies a batch row by row: the second row updates the first.
    [Fact]
    public void Upsert_MySql_KeyRepeatedInBatch_AppliesRowsInOrder()
    {
        RandomUsersTable t = new("");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Age).Values(600, 1).Values(600, 2)"
                    + ".OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))",
                () => InsertInto(t, t.Id, t.Age)
                    .Values(600, 1)
                    .Values(600, 2)
                    .OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))),
            UpsertMergeTwins.ReadAges(600));

        Assert.Null(upsert.Error);
        Assert.Equal(["600|2"], row);
    }

    // The row meets row 1 by id and row 2 by code; 8.0.46 updates row 1 alone, a pick the
    // reference does not model, so a proposed row meets one row at most (UpsertGenerators).
    [Fact]
    public void Upsert_MySql_ConflictOnTwoRows_UpdatesOne()
    {
        RandomUsersTable t = new("");
        (Outcome upsert, List<string> rows) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Code, t.Age).Values(1, 102, 77)"
                    + ".OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))",
                () => InsertInto(t, t.Id, t.Code, t.Age)
                    .Values(1, 102, 77)
                    .OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))),
            UpsertMergeTwins.ReadAges(1, 2));

        Assert.Null(upsert.Error);
        Assert.Equal(["1|77", "2|25"], rows);
    }

    // The existing row's age is 30, so reading the starting row would store 30 in dept_id; 8.0.46
    // evaluates the assignments left to right, as its UPDATE does, and stores 31.
    [Fact]
    public void OnDuplicateKeyUpdate_MySql_LaterAssignment_ReadsEarlierAssignedValue()
    {
        RandomUsersTable t = new("");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id).Values(1).OnDuplicateKeyUpdate(t.Age == t.Age + 1, "
                    + "t.DeptId == t.Age)",
                () => InsertInto(t, t.Id)
                    .Values(1)
                    .OnDuplicateKeyUpdate(t.Age == t.Age + 1, t.DeptId == t.Age)),
            new PlannedStatement(
                "READ",
                "Select(t.Id, t.Age, t.DeptId).From(t).Where(t.Id == 1)",
                () => Select(t.Id, t.Age, t.DeptId).From(t).Where(t.Id == 1)));

        Assert.Null(upsert.Error);
        Assert.Equal(["1|31|31"], row);
    }

    // The starting rows give row 1 the seed sum, 508; 8.0.46 counts the row inserted before it,
    // so a batch's subqueries read rt_depts there.
    [Fact]
    public void OnDuplicateKeyUpdate_MySql_BatchSubqueryOverTarget_ReadsEarlierRows()
    {
        RandomUsersTable t = new("");
        RandomUsersTable u2 = new("u2");
        (Outcome upsert, List<string> row) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertInto(t, t.Id, t.Age).Values(600, 100).Values(1, 0)"
                    + ".OnDuplicateKeyUpdate(t.Age == Select(Sum(u2.Age)).From(u2))",
                () => InsertInto(t, t.Id, t.Age)
                    .Values(600, 100)
                    .Values(1, 0)
                    .OnDuplicateKeyUpdate(t.Age == Select(Sum(u2.Age)).From(u2))),
            UpsertMergeTwins.ReadAges(1));

        Assert.Null(upsert.Error);
        Assert.Equal(["1|608"], row);
    }

    // The row alias holds the listed columns alone (8.0.46).
    [Fact]
    public void OnDuplicateKeyUpdate_MySql_ExcludedUnlistedColumn_IsRejectedByTheEngine()
    {
        RandomUsersTable t = new("");
        string? error = Rejection(UpsertMergeTwins.Statement(
            "InsertInto(t, t.Id).Values(1).OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))",
            () => InsertInto(t, t.Id).Values(1).OnDuplicateKeyUpdate(t.Age == Excluded(t.Age))));

        Assert.Contains("Unknown column 'new.age'", error);
    }

    // SqlArtisan renders the target's column bare, which 8.0.46 reads as ambiguous beside the
    // row alias's listed one; the generators avoid it while it stands (a candidate defect).
    [Fact]
    public void OnDuplicateKeyUpdate_MySql_BareListedTargetColumn_IsRejectedByTheEngine()
    {
        RandomUsersTable t = new("");
        string? error = Rejection(UpsertMergeTwins.Statement(
            "InsertInto(t, t.Id, t.Age).Values(1, 5).OnDuplicateKeyUpdate(t.Age == t.Age + 1)",
            () => InsertInto(t, t.Id, t.Age)
                .Values(1, 5)
                .OnDuplicateKeyUpdate(t.Age == t.Age + 1)));

        Assert.Contains("Column 'age' in field list is ambiguous", error);
    }

    // 8.0.46's IGNORE skips the conflicting row, but also stores NULL in the NOT NULL key as 0
    // and truncates a value past the column's length, which no generated row writes.
    [Fact]
    public void InsertIgnore_MySql_InvalidValues_AreCoerced()
    {
        RandomUsersTable t = new("");
        string long50 = new('z', 50);
        PlannedStatement read = new(
            "READ",
            "Select(t.Id, t.Name, t.Age).From(t).Where(t.Id.In(0, 1, 600))",
            () => Select(t.Id, t.Name, t.Age).From(t).Where(t.Id.In(0, 1, 600)));
        (Outcome skipped, List<string> kept) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertIgnoreInto(t, t.Id, t.Age).Values(1, 99)",
                () => InsertIgnoreInto(t, t.Id, t.Age).Values(1, 99)),
            read);
        (Outcome nulled, List<string> zero) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertIgnoreInto(t, t.Id, t.Name).Values(Null, \"x\")",
                () => InsertIgnoreInto(t, t.Id, t.Name).Values(Null, "x")),
            read);
        (Outcome truncated, List<string> cut) = RunAndRead(
            UpsertMergeTwins.Statement(
                "InsertIgnoreInto(t, t.Id, t.Name).Values(600, new string('z', 50))",
                () => InsertIgnoreInto(t, t.Id, t.Name).Values(600, long50)),
            read);

        Assert.Null(skipped.Error);
        Assert.Equal(["1|'Alice'|30"], kept);
        Assert.Null(nulled.Error);
        Assert.Equal(["0|'x'|NULL", "1|'Alice'|30"], zero.Order(StringComparer.Ordinal));
        Assert.Null(truncated.Error);
        Assert.Contains($"600|'{long50[..40]}'|NULL", cut);
    }

    [Fact]
    public void UpdateSet_MySql_KeyShiftedOntoNextRow_IsRejectedByTheEngine()
    {
        Assert.Contains("Duplicate entry", Rejection(UpsertMergeTwins.ShiftedCodes()));
    }
}

public sealed partial class SqlServerCombinatorialDmlTests
{
    [Fact]
    public void Merge_SqlServer_TargetMatchedTwice_IsRejectedByTheEngine()
    {
        Assert.Contains(
            "attempted to UPDATE or DELETE the same row more than once",
            Rejection(UpsertMergeTwins.TargetMatchedTwice()));
    }
}

public sealed partial class OracleCombinatorialDmlTests
{
    [Fact]
    public void Merge_Oracle_TargetMatchedTwice_IsRejectedByTheEngine()
    {
        Assert.Contains("ORA-30926", Rejection(UpsertMergeTwins.TargetMatchedTwice()));
    }

    // No generated MERGE names a target column in ON beside the id, which no branch sets.
    [Fact]
    public void Merge_Oracle_UpdateOfOnColumn_IsRejectedByTheEngine()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable s = new("s");
        string? error = Rejection(UpsertMergeTwins.Statement(
            "MergeInto(u).Using(s).On((u.Id == s.Id) & (u.Age > 0)).WhenMatched()"
                + ".ThenUpdateSet(u.Age == s.Age)",
            () => MergeInto(u)
                .Using(s)
                .On((u.Id == s.Id) & (u.Age > 0))
                .WhenMatched()
                .ThenUpdateSet(u.Age == s.Age)));

        Assert.Contains("ORA-38104", error);
    }

    // XE 21.3.0 rejects a row-limited subquery in the INSERT's VALUES, naming its ORDER BY key;
    // an aggregate one over the same rows runs.
    [Fact]
    public void MergeInsertValues_Oracle_RowLimitedSubquery_IsRejectedByTheEngine()
    {
        RandomDeptsTable d2 = new("d2");
        string? rejected = Rejection(UpsertMergeTwins.MergeInsertValue(
            "Select(d2.Id).From(d2).Where(d2.Id == s.Age).OrderBy(d2.Id).FetchFirst(1)",
            age => Select(d2.Id).From(d2).Where(d2.Id == age).OrderBy(d2.Id).FetchFirst(1)));
        string? accepted = Rejection(UpsertMergeTwins.MergeInsertValue(
            "Select(Max(d2.Id)).From(d2).Where(d2.Id == s.Age)",
            age => Select(Max(d2.Id)).From(d2).Where(d2.Id == age)));

        Assert.Contains("ORA-38101", rejected);
        Assert.Null(accepted);
    }
}

public sealed partial class Oracle23aiCombinatorialDmlTests
{
    // Free 23ai runs FETCH FIRST in the INSERT's VALUES but rejects OFFSET ... FETCH NEXT there,
    // naming its ORDER BY key, as XE 21.3.0 rejects both.
    [Fact]
    public void MergeInsertValues_Oracle_OffsetSubquery_IsRejectedByTheEngine()
    {
        RandomDeptsTable d2 = new("d2");
        string? rejected = Rejection(UpsertMergeTwins.MergeInsertValue(
            "Select(d2.Id).From(d2).Where(d2.Id == s.Age).OrderBy(d2.Id).OffsetRows(0)"
                + ".FetchNext(1)",
            age => Select(d2.Id)
                .From(d2)
                .Where(d2.Id == age)
                .OrderBy(d2.Id)
                .OffsetRows(0)
                .FetchNext(1)));
        string? accepted = Rejection(UpsertMergeTwins.MergeInsertValue(
            "Select(d2.Id).From(d2).Where(d2.Id == s.Age).OrderBy(d2.Id).FetchFirst(1)",
            age => Select(d2.Id).From(d2).Where(d2.Id == age).OrderBy(d2.Id).FetchFirst(1)));

        Assert.Contains("ORA-38101", rejected);
        Assert.Null(accepted);
    }
}

/// <summary>The statements the upsert and MERGE twins share across lanes.</summary>
internal static class UpsertMergeTwins
{
    public static PlannedStatement Statement(string csharp, Func<ISqlBuilder> build) =>
        new("STATEMENT", csharp, build);

    public static PlannedStatement ReadAges(params int[] ids)
    {
        RandomUsersTable t = new("");
        object[] keys = [.. ids.Cast<object>()];
        return new PlannedStatement(
            "READ",
            "Select(t.Id, t.Age).From(t).Where(t.Id.In(...)).OrderBy(t.Id)",
            () => Select(t.Id, t.Age).From(t).Where(t.Id.In(keys)).OrderBy(t.Id));
    }

    // A fresh id with seed row 2's code meets no row by the arbiter.
    public static PlannedStatement OtherKeyConflict()
    {
        RandomUsersTable t = new("");
        return Statement(
            "InsertInto(t, t.Id, t.Code).Values(600, 102).OnConflict(t.Id)"
                + ".DoUpdateSet(t.Age == 1)",
            () => InsertInto(t, t.Id, t.Code)
                .Values(600, 102)
                .OnConflict(t.Id)
                .DoUpdateSet(t.Age == 1));
    }

    // Codes 101 and 102 sit on consecutive rows, so row 1's new code meets row 2's old one.
    public static PlannedStatement ShiftedCodes()
    {
        RandomUsersTable t = new("");
        return Statement(
            "Update(t).Set(t.Code == t.Code + 1)",
            () => Update(t).Set(t.Code == t.Code + 1));
    }

    // Source rows 19..28 meet no target and insert ids 29..38, aged by value(s.Age).
    public static PlannedStatement MergeInsertValue(
        string csharp,
        Func<SqlExpression, object> value)
    {
        RandomUsersTable u = new("u");
        RandomUsersTable s = new("s");
        return Statement(
            "MergeInto(u).Using(s).On(u.Id == s.Id + 10).WhenNotMatched()"
                + $".ThenInsert(u.Id, u.Age).Values(s.Id + 10, {csharp})",
            () => MergeInto(u)
                .Using(s)
                .On(u.Id == s.Id + 10)
                .WhenNotMatched()
                .ThenInsert(u.Id, u.Age)
                .Values(s.Id + 10, value(s.Age)));
    }

    // dept_id 1 belongs to six seed rows, so target row 1 meets six source rows.
    public static PlannedStatement TargetMatchedTwice()
    {
        RandomUsersTable u = new("u");
        RandomUsersTable s = new("s");
        return Statement(
            "MergeInto(u).Using(s).On(u.Id == s.DeptId).WhenMatched()"
                + ".ThenUpdateSet(u.Age == s.Age)",
            () => MergeInto(u)
                .Using(s)
                .On(u.Id == s.DeptId)
                .WhenMatched()
                .ThenUpdateSet(u.Age == s.Age));
    }
}
