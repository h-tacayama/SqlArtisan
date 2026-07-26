using Microsoft.Data.Sqlite;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// SQLite runs in-process (no container), so the generator's SQLite path — the one
// engine that cannot use the information_schema builder — is verified here in the
// fast unit lane. MySQL/SQL Server are verified against live engines in the
// integration suite.
public class SqliteTableInfoRepositoryTests
{
    private const string Schema =
        """
        CREATE TABLE order_line (order_id INTEGER, quantity INTEGER);
        CREATE TABLE user_account (
            id INTEGER PRIMARY KEY,
            user_name TEXT NOT NULL,
            created_at TEXT);
        """;

    [Fact]
    public void GetAllTables_ReturnsTablesAndColumnsInOrder()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<DbTableInfo> tables =
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        Assert.Equal(["order_line", "user_account"], tables.Select(t => t.TableName));

        DbTableInfo userAccount = tables[1];
        Assert.Equal("UserAccountTable", userAccount.ClassName);
        Assert.Equal(
            ["id", "user_name", "created_at"],
            userAccount.Columns.Select(c => c.Name));
        Assert.Equal(
            ["Id", "UserName", "CreatedAt"],
            userAccount.Columns.Select(c => c.PascalCaseName));
        Assert.Equal(
            ["INTEGER", "TEXT", "TEXT"],
            userAccount.Columns.Select(c => c.DataType));
    }

    [Fact]
    public void GetAllTables_LowercaseNames_LowercasesCatalogNames()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE AppConfig (ConfigKey TEXT, ConfigValue TEXT);");

        IReadOnlyList<DbTableInfo> tables =
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: true)
                .GetAllTables();

        DbTableInfo table = Assert.Single(tables);
        Assert.Equal("appconfig", table.TableName);
        Assert.Equal(["configkey", "configvalue"], table.Columns.Select(c => c.Name));
    }

    [Fact]
    public void GetAllTables_ExcludesInternalSqliteTables()
    {
        // An INTEGER PRIMARY KEY column aliases the rowid, and AUTOINCREMENT forces
        // the internal sqlite_sequence table — which must not appear as a source.
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT);");

        IReadOnlyList<DbTableInfo> tables =
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        DbTableInfo table = Assert.Single(tables);
        Assert.Equal("item", table.TableName);
    }

    [Fact]
    public void GetAllTables_CollectsNullabilityAndDefaults()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE item (
                code TEXT NOT NULL,
                note TEXT,
                qty INTEGER NOT NULL DEFAULT 0);
            """);

        DbTableInfo table = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([false, true, false], table.Columns.Select(c => c.IsNullable));
        Assert.Equal([false, false, true], table.Columns.Select(c => c.HasDefault));
    }

    [Fact]
    public void GetAllTables_RowIdAlias_IsNotNullableAndDefaulted()
    {
        // The pragma reports a lone INTEGER PRIMARY KEY as nullable with no default,
        // though it never holds NULL and is auto-assigned.
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT NOT NULL);");

        DbTableInfo table = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        DbColumnInfo id = table.Columns[0];
        Assert.False(id.IsNullable);
        Assert.True(id.HasDefault);
    }

    [Fact]
    public void GetAllTables_CompositeKey_IsNotTreatedAsRowIdAlias()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE pair (a INTEGER NOT NULL, b INTEGER NOT NULL, PRIMARY KEY (a, b));");

        DbTableInfo table = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([false, false], table.Columns.Select(c => c.HasDefault));
    }

    // INTEGER PRIMARY KEY DESC is a real key, not a rowid alias: it accepts NULL and
    // nothing is auto-assigned, though table_info reports it identically to an alias.
    [Fact]
    public void GetAllTables_DescendingIntegerKey_IsNotTreatedAsRowIdAlias()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY DESC, name TEXT);");

        DbColumnInfo id = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables())
            .Columns[0];

        Assert.True(id.IsNullable);
        Assert.False(id.HasDefault);
    }

    [Fact]
    public void GetAllTables_WithoutRowId_KeyIsNotNullableAndNotDefaulted()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, name TEXT) WITHOUT ROWID;");

        DbColumnInfo id = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables())
            .Columns[0];

        Assert.False(id.IsNullable);
        Assert.False(id.HasDefault);
    }

    [Fact]
    public void GetAllTables_WithoutRowIdCompositeKey_KeyColumnsAreNotNullable()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE pair (a TEXT, b TEXT, c TEXT, PRIMARY KEY (a, b)) WITHOUT ROWID;");

        DbTableInfo table = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([false, false, true], table.Columns.Select(c => c.IsNullable));
    }

    [Fact]
    public void GetAllTables_TextSingleKey_IsNotTreatedAsRowIdAlias()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (code TEXT NOT NULL PRIMARY KEY, name TEXT);");

        DbColumnInfo code = Assert.Single(
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables())
            .Columns[0];

        Assert.False(code.IsNullable);
        Assert.False(code.HasDefault);
    }

    [Fact]
    public void GeneratedCode_Compiles()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<DbTableInfo> tables =
            new SqliteTableInfoRepository(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        GeneratedCodeCompiler.AssertCompiles(
            tables.Select(t => t.GenerateCode(TestSettings.Create())));
    }
}
