using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The drift lane end to end: generate against a live (SQLite) schema, then let the
// schema and the committed files diverge and assert what --check reports.
public class TableClassGeneratorTests : IDisposable
{
    private const string Schema =
        """
        CREATE TABLE item (id INTEGER PRIMARY KEY, code TEXT NOT NULL);
        CREATE TABLE tag (id INTEGER PRIMARY KEY, label TEXT);
        """;

    private readonly string _outputDirectory = Path.Combine(
        Path.GetTempPath(),
        $"sqlartisan_tcg_out_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_Generate_WritesEveryTableThenChecksClean()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> generated = Run(db, RunMode.Generate);

        Assert.Equal([TableStatus.Added, TableStatus.Added], generated.Select(r => r.Status));
        Assert.All(generated, r => Assert.True(File.Exists(r.Path)));

        Assert.All(Run(db, RunMode.Check), r => Assert.Equal(TableStatus.Unchanged, r.Status));
    }

    [Fact]
    public void Run_DryRun_WritesNothing()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Generate, dryRun: true);

        Assert.All(results, r => Assert.False(File.Exists(r.Path)));
    }

    [Fact]
    public void Run_Check_AddedColumn_ReportsModifiedWithTheColumn()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["+ note"], item.Changes);
    }

    [Fact]
    public void Run_Check_MetadataOnlyChange_ReportsModifiedWithoutNamingColumns()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            "CREATE TABLE item (id INTEGER PRIMARY KEY, code TEXT);");
        Run(db, RunMode.Generate);

        // Same columns, different nullability: only the emitted metadata moves.
        db.Execute(
            """
            CREATE TABLE item_new (id INTEGER PRIMARY KEY, code TEXT NOT NULL);
            DROP TABLE item;
            ALTER TABLE item_new RENAME TO item;
            """);

        TableResult item = Single(Run(db, RunMode.Check), "item");

        Assert.Equal(TableStatus.Modified, item.Status);
        Assert.Equal(["~ column metadata or layout changed"], item.Changes);
    }

    [Fact]
    public void Run_Check_MissingFile_ReportsAdded()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        File.Delete(Path.Combine(_outputDirectory, "ItemTable.cs"));

        Assert.Equal(TableStatus.Added, Single(Run(db, RunMode.Check), "item").Status);
    }

    [Fact]
    public void Run_Check_DroppedTable_ReportsRemoved()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        db.Execute("DROP TABLE tag");

        TableResult removed = Assert.Single(
            Run(db, RunMode.Check).Where(r => r.Status == TableStatus.Removed));

        Assert.Equal("TagTable.cs", removed.TableName);
    }

    [Fact]
    public void Run_Check_HandWrittenFile_IsNotReportedAsRemoved()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        File.WriteAllText(
            Path.Combine(_outputDirectory, "Helpers.cs"),
            "namespace Generated.Tables;\n\ninternal static class Helpers { }\n");

        Assert.DoesNotContain(Run(db, RunMode.Check), r => r.Status == TableStatus.Removed);
    }

    [Fact]
    public void Run_Fix_RewritesTheDriftedFileOnly()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);

        string tagPath = Path.Combine(_outputDirectory, "TagTable.cs");
        DateTime tagWrittenAt = File.GetLastWriteTimeUtc(tagPath);
        db.Execute("ALTER TABLE item ADD COLUMN note TEXT");

        Run(db, RunMode.Fix);

        Assert.All(Run(db, RunMode.Check), r => Assert.Equal(TableStatus.Unchanged, r.Status));
        Assert.Equal(tagWrittenAt, File.GetLastWriteTimeUtc(tagPath));
    }

    [Fact]
    public void Run_Tables_LimitsTheRunToTheNamedTables()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        IReadOnlyList<TableResult> results = Run(db, RunMode.Generate, tableNames: ["item"]);

        TableResult only = Assert.Single(results);
        Assert.Equal("item", only.TableName);
        Assert.False(File.Exists(Path.Combine(_outputDirectory, "TagTable.cs")));
    }

    // A scoped run never looked at the other tables, so it must not conclude their
    // files are orphans.
    [Fact]
    public void Run_Tables_DoesNotReportRemovedFiles()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);
        Run(db, RunMode.Generate);
        db.Execute("DROP TABLE tag");

        Assert.DoesNotContain(
            Run(db, RunMode.Check, tableNames: ["item"]),
            r => r.Status == TableStatus.Removed);
    }

    [Fact]
    public void Run_UnknownTable_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(Schema);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate, tableNames: ["nope"]));

        Assert.Equal("--tables names 'nope', which the schema does not contain", ex.Message);
    }

    // Unguarded, the second table overwrote the first's file and disappeared,
    // leaving a drift that --fix could not clear.
    [Fact]
    public void Run_TwoTablesWithOneClassName_ThrowsCommandLineException()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE dupe_class (id INTEGER);
            CREATE TABLE dupe__class (id INTEGER);
            """);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => Run(db, RunMode.Generate));

        Assert.Equal(
            "Tables 'dupe__class' and 'dupe_class' both generate the class DupeClassTable; "
                + "rename one of them or narrow the run with --tables.",
            ex.Message);
    }

    // The escape hatch the message names has to work.
    [Fact]
    public void Run_TwoTablesWithOneClassName_NarrowedByTables_Generates()
    {
        using TempSqliteDatabase db = TempSqliteDatabase.Create(
            """
            CREATE TABLE dupe_class (id INTEGER);
            CREATE TABLE dupe__class (id INTEGER);
            """);

        TableResult only = Assert.Single(
            Run(db, RunMode.Generate, tableNames: ["dupe_class"]));

        Assert.Equal("dupe_class", only.TableName);
    }

    private IReadOnlyList<TableResult> Run(
        TempSqliteDatabase db,
        RunMode mode,
        bool dryRun = false,
        IReadOnlyList<string>? tableNames = null)
    {
        RunOptions options = new(
            mode,
            db.ConnectionInfo,
            TestSettings.Create(outputDirectory: _outputDirectory, tableNames: tableNames),
            dryRun);

        return new TableClassGenerator(
            CatalogReaderFactory.Create(db.ConnectionInfo, lowercaseNames: false),
            options).Run();
    }

    private static TableResult Single(IReadOnlyList<TableResult> results, string tableName) =>
        Assert.Single(results.Where(r => r.TableName == tableName));
}
