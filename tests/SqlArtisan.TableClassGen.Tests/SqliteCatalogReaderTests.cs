using System.Globalization;
using Microsoft.Data.Sqlite;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// SQLite runs in-process (no container), so its reader — one of the two that
// cannot use the information_schema builder, Oracle's being the other — is
// verified here in the fast unit lane rather than the integration suite.
public class SqliteCatalogReaderTests
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

        IReadOnlyList<CatalogTable> tables =
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        Assert.Equal(["order_line", "user_account"], tables.Select(t => t.TableName));

        CatalogTable userAccount = tables[1];
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

        IReadOnlyList<CatalogTable> tables =
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: true)
                .GetAllTables();

        CatalogTable table = Assert.Single(tables);
        Assert.Equal("appconfig", table.TableName);
        Assert.Equal(["configkey", "configvalue"], table.Columns.Select(c => c.Name));
    }

    // Under tr-TR, culture-sensitive ToLower() turns "BILLING" into "bıllıng"
    // (dotless ı) rather than "billing" — a table name pragma_table_info's
    // case-insensitive ASCII lookup no longer matches, so the table went missing
    // from the results entirely rather than merely being cased wrong.
    [Fact]
    public void GetAllTables_LowercaseNamesUnderTurkishCulture_UsesInvariantCasing()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

        try
        {
            using TempSqliteDatabase db = TempSqliteDatabase.Create(
                "CREATE TABLE BILLING (TITLE TEXT);");

            IReadOnlyList<CatalogTable> tables =
                new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: true)
                    .GetAllTables();

            CatalogTable table = Assert.Single(tables);
            Assert.Equal("billing", table.TableName);
            Assert.Equal(["title"], table.Columns.Select(c => c.Name));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetAllTables_ExcludesInternalSqliteTables()
    {
        // An INTEGER PRIMARY KEY column aliases the rowid, and AUTOINCREMENT forces
        // the internal sqlite_sequence table — which must not appear as a source.
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT);");

        IReadOnlyList<CatalogTable> tables =
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        CatalogTable table = Assert.Single(tables);
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

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
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

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        CatalogColumn id = table.Columns[0];
        Assert.False(id.IsNullable);
        Assert.True(id.HasDefault);
    }

    [Fact]
    public void GetAllTables_CompositeKey_IsNotTreatedAsRowIdAlias()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE pair (a INTEGER NOT NULL, b INTEGER NOT NULL, PRIMARY KEY (a, b));");

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
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

        CatalogColumn id = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
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

        CatalogColumn id = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
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

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([false, false, true], table.Columns.Select(c => c.IsNullable));
    }

    [Fact]
    public void GetAllTables_TextSingleKey_IsNotTreatedAsRowIdAlias()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (code TEXT NOT NULL PRIMARY KEY, name TEXT);");

        CatalogColumn code = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables())
            .Columns[0];

        Assert.False(code.IsNullable);
        Assert.False(code.HasDefault);
    }

    // The whole #266 matrix in one table: only a leading column of a plain index
    // may be claimed, and the column an index expression names claims nothing.
    [Fact]
    public void GetAllTables_IndexedIsLeadingColumnOnly()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE doc (
                id INTEGER PRIMARY KEY,
                code TEXT,
                email TEXT,
                a TEXT,
                b TEXT,
                plain TEXT);
            CREATE INDEX ix_code ON doc(code);
            CREATE INDEX ix_ab ON doc(a, b);
            CREATE INDEX ix_expr ON doc(upper(email));
            """);

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal(
            [true, true, null, true, false, false],
            table.Columns.Select(c => c.IsIndexed));
    }

    // The rowid alias carries no index row of its own, yet EXPLAIN QUERY PLAN
    // reports SEARCH ... USING INTEGER PRIMARY KEY for a predicate on it.
    [Fact]
    public void GetAllTables_RowIdAlias_IsIndexed()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE note (id INTEGER PRIMARY KEY, body TEXT);");

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([true, false], table.Columns.Select(c => c.IsIndexed));
    }

    // A plain index's DDL names its own column, so scanning every index would mark
    // each indexed column unknown; only expression-bearing indexes are scanned.
    [Fact]
    public void GetAllTables_PlainIndexDdl_DoesNotSuppressItsOwnColumn()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE person (name TEXT, nickname TEXT);
            CREATE INDEX ix_name ON person(name);
            """);

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([true, false], table.Columns.Select(c => c.IsIndexed));
    }

    // Whether the partial predicate covers a query is an expression to interpret,
    // so a partial-only lead claims nothing; a full-index lead beside it still does.
    [Fact]
    public void GetAllTables_PartialIndex_LeadingColumnIsUnknown()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE task (status TEXT, amount INTEGER, owner TEXT);
            CREATE INDEX ix_status ON task(status) WHERE amount > 0;
            CREATE INDEX ix_owner_full ON task(owner);
            CREATE INDEX ix_owner_part ON task(owner) WHERE amount > 0;
            """);

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([null, false, true], table.Columns.Select(c => c.IsIndexed));
    }

    [Fact]
    public void GetAllTables_UniqueIndex_LeadsLikeAnyOther()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE account (login TEXT, region TEXT);
            CREATE UNIQUE INDEX ux_login ON account(login);
            """);

        CatalogTable table = Assert.Single(
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables());

        Assert.Equal([true, false], table.Columns.Select(c => c.IsIndexed));
    }

    [Fact]
    public void GeneratedCode_Compiles()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<CatalogTable> tables =
            new SqliteCatalogReader(db.ConnectionInfo, lowercaseNames: false)
                .GetAllTables();

        TableClassEmitter emitter = new(TestSettings.Create());

        GeneratedCodeCompiler.AssertCompiles(tables.Select(emitter.Emit));
    }
}
